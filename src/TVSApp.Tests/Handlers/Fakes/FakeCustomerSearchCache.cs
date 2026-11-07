using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Search;

namespace TVSApp.Tests.Handlers.Fakes;

public class FakeCustomerSearchCache : ICustomerSearchCache
{
    public List<CustomerSearchEntry> Entries { get; } = new();
    public int InvalidateCalls { get; private set; }
    public int GetEntriesCalls { get; private set; }

    public FakeCustomerSearchCache Add(long id, string name, string phone = "")
    {
        Entries.Add(CustomerSearchEntry.Create(id, name, phone));
        return this;
    }

    public Task<IReadOnlyList<CustomerSearchEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        GetEntriesCalls++;
        IReadOnlyList<CustomerSearchEntry> ordered = Entries.OrderByDescending(e => e.Id).ToList();
        return Task.FromResult(ordered);
    }

    public void Invalidate() => InvalidateCalls++;
}
