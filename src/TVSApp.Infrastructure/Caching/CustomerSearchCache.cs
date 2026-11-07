using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Search;

namespace TVS_App.Infrastructure.Caching;

/// <summary>
/// Índice de busca de clientes em <see cref="IMemoryCache"/> (singleton).
/// - Montado sob demanda, uma única vez mesmo com buscas simultâneas (semáforo + double-check).
/// - Expira em 1 hora (rede de segurança para alterações feitas fora do sistema).
/// - <see cref="Invalidate"/> incrementa a geração: uma montagem que começou antes da invalidação
///   devolve o resultado à própria requisição, mas não o grava no cache (evita dado velho).
/// - Se a leitura do banco falhar, nada é gravado e a próxima busca tenta de novo.
/// </summary>
public sealed class CustomerSearchCache : ICustomerSearchCache, IDisposable
{
    public const string CacheKey = "customers:search-index";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;
    private readonly ICustomerSearchSource _source;
    private readonly ILogger<CustomerSearchCache> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private long _generation;

    public CustomerSearchCache(
        IMemoryCache cache,
        ICustomerSearchSource source,
        ILogger<CustomerSearchCache> logger,
        TimeProvider timeProvider)
    {
        _cache = cache;
        _source = source;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<CustomerSearchEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        if (TryGetValid(out var entries))
            return entries;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetValid(out entries))
                return entries;

            var generation = Interlocked.Read(ref _generation);
            var stopwatch = Stopwatch.StartNew();

            var loaded = (await _source.LoadAsync(cancellationToken)).AsReadOnly();

            if (Interlocked.Read(ref _generation) == generation)
            {
                var snapshot = new Snapshot(loaded, _timeProvider.GetUtcNow() + Lifetime);
                _cache.Set(CacheKey, snapshot, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Lifetime });
            }

            _logger.LogInformation("Índice de busca de clientes montado: {Count} clientes em {ElapsedMs} ms",
                loaded.Count, stopwatch.ElapsedMilliseconds);

            return loaded;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _generation);
        _cache.Remove(CacheKey);
        _logger.LogDebug("Índice de busca de clientes invalidado");
    }

    private bool TryGetValid(out IReadOnlyList<CustomerSearchEntry> entries)
    {
        if (_cache.TryGetValue(CacheKey, out Snapshot? snapshot)
            && snapshot is not null
            && _timeProvider.GetUtcNow() < snapshot.ExpiresAt)
        {
            entries = snapshot.Entries;
            return true;
        }

        entries = [];
        return false;
    }

    public void Dispose() => _loadLock.Dispose();

    private sealed record Snapshot(IReadOnlyList<CustomerSearchEntry> Entries, DateTimeOffset ExpiresAt);
}
