using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TVS_App.Application.Search;
using TVS_App.Infrastructure.Data;

namespace TVS_App.Infrastructure.Caching;

/// <summary>Lê do banco os dados do índice de busca de clientes.</summary>
public interface ICustomerSearchSource
{
    /// <summary>Todos os clientes, ordenados por Id decrescente.</summary>
    Task<List<CustomerSearchEntry>> LoadAsync(CancellationToken cancellationToken);
}

public sealed class CustomerSearchSource : ICustomerSearchSource
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CustomerSearchSource(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<List<CustomerSearchEntry>> LoadAsync(CancellationToken cancellationToken)
    {
        // O cache é singleton e o DbContext é scoped: cada montagem abre o próprio escopo.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDataContext>();

        var rows = await context.Customers
            .AsNoTracking()
            .OrderByDescending(c => c.Id)
            .Select(c => new { c.Id, Name = c.Name.CustomerName, Phone = c.Phone.CustomerPhone })
            .ToListAsync(cancellationToken);

        return rows.Select(r => CustomerSearchEntry.Create(r.Id, r.Name, r.Phone)).ToList();
    }
}
