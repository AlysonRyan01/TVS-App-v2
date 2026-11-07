using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using TVS_App.Application.Search;
using TVS_App.Infrastructure.Caching;

namespace TVSApp.Tests.Infrastructure;

[TestClass]
public class CustomerSearchCacheTests
{
    private FakeCustomerSearchSource _source = null!;
    private ManualTimeProvider _time = null!;
    private MemoryCache _memoryCache = null!;
    private CustomerSearchCache _cache = null!;

    [TestInitialize]
    public void Setup()
    {
        _source = new FakeCustomerSearchSource();
        _source.Set(CustomerSearchEntry.Create(1, "JOAO", ""));
        _time = new ManualTimeProvider();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cache = new CustomerSearchCache(_memoryCache, _source, NullLogger<CustomerSearchCache>.Instance, _time);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _cache.Dispose();
        _memoryCache.Dispose();
    }

    [TestMethod]
    public async Task SecondCall_UsesCache()
    {
        await _cache.GetEntriesAsync();
        var entries = await _cache.GetEntriesAsync();

        Assert.AreEqual(1, _source.LoadCalls);
        Assert.AreEqual(1, entries.Single().Id);
    }

    [TestMethod]
    public async Task Invalidate_NextCallReloadsFromSource()
    {
        await _cache.GetEntriesAsync();
        _source.Set(CustomerSearchEntry.Create(2, "MARIA", ""), CustomerSearchEntry.Create(1, "JOAO", ""));

        _cache.Invalidate();
        var entries = await _cache.GetEntriesAsync();

        Assert.AreEqual(2, _source.LoadCalls);
        CollectionAssert.AreEqual(new long[] { 2, 1 }, entries.Select(e => e.Id).ToArray());
    }

    [TestMethod]
    public async Task InvalidateDuringLoad_StaleResultIsNotCached()
    {
        var gate = _source.HoldNextLoad();
        var inFlight = _cache.GetEntriesAsync();
        await _source.WaitUntilLoading();

        // Cliente novo gravado enquanto a montagem lia o banco (com dado antigo).
        _cache.Invalidate();
        gate.SetResult();
        var stale = await inFlight;

        _source.Set(CustomerSearchEntry.Create(2, "NOVO", ""), CustomerSearchEntry.Create(1, "JOAO", ""));
        var fresh = await _cache.GetEntriesAsync();

        Assert.AreEqual(1, stale.Count);
        Assert.AreEqual(2, _source.LoadCalls);
        Assert.AreEqual(2, fresh[0].Id);
    }

    [TestMethod]
    public async Task ConcurrentCalls_LoadOnlyOnce()
    {
        var gate = _source.HoldNextLoad();
        var calls = Enumerable.Range(0, 10).Select(_ => _cache.GetEntriesAsync()).ToArray();
        await _source.WaitUntilLoading();

        gate.SetResult();
        var results = await Task.WhenAll(calls);

        Assert.AreEqual(1, _source.LoadCalls);
        Assert.IsTrue(results.All(r => r.Single().Id == 1));
    }

    [TestMethod]
    public async Task LoadFails_NothingCached_NextCallRetries()
    {
        _source.FailNextLoad = new InvalidOperationException("banco fora");

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _cache.GetEntriesAsync());
        var entries = await _cache.GetEntriesAsync();

        Assert.AreEqual(2, _source.LoadCalls);
        Assert.AreEqual(1, entries.Single().Id);
    }

    [TestMethod]
    public async Task AfterOneHour_Reloads()
    {
        await _cache.GetEntriesAsync();

        _time.Advance(TimeSpan.FromMinutes(59));
        await _cache.GetEntriesAsync();
        Assert.AreEqual(1, _source.LoadCalls);

        _time.Advance(TimeSpan.FromMinutes(2));
        await _cache.GetEntriesAsync();
        Assert.AreEqual(2, _source.LoadCalls);
    }

    private sealed class FakeCustomerSearchSource : ICustomerSearchSource
    {
        private List<CustomerSearchEntry> _entries = new();
        private TaskCompletionSource? _gate;
        private readonly TaskCompletionSource _loading = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _loadCalls;

        public int LoadCalls => _loadCalls;
        public Exception? FailNextLoad { get; set; }

        public void Set(params CustomerSearchEntry[] entries) => _entries = entries.ToList();

        public TaskCompletionSource HoldNextLoad()
        {
            _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return _gate;
        }

        public Task WaitUntilLoading() => _loading.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public async Task<List<CustomerSearchEntry>> LoadAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _loadCalls);
            var snapshot = _entries.ToList(); // lê "o banco" no início, como a consulta real
            _loading.TrySetResult();

            if (_gate is { } gate)
            {
                _gate = null;
                await gate.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }

            if (FailNextLoad is { } failure)
            {
                FailNextLoad = null;
                throw failure;
            }

            return snapshot;
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
