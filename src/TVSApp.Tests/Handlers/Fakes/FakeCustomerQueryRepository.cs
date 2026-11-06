using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;

namespace TVSApp.Tests.Handlers.Fakes;

public class FakeCustomerQueryRepository : ICustomerQueryRepository
{
    public List<Customer> Customers { get; } = new();
    public int? LastLimit { get; private set; }
    public CustomerSearchFilter? LastSearchFilter { get; private set; }
    public int? LastSearchPage { get; private set; }
    public int? LastSearchPageSize { get; private set; }

    public Task<BaseResponse<Customer?>> GetByIdAsync(long id)
    {
        var customer = Customers.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(customer is null
            ? new BaseResponse<Customer?>(null, 404, "Cliente não encontrado.")
            : new BaseResponse<Customer?>(customer));
    }

    public Task<BaseResponse<List<Customer>>> GetCustomerByName(string name, int limit)
    {
        LastLimit = limit;
        var term = name.Trim().ToUpper();
        var found = Customers.Where(c => c.Name.CustomerName.Contains(term)).Take(limit).ToList();
        return Task.FromResult(new BaseResponse<List<Customer>>(found, 200, "Clientes obtidos com sucesso!"));
    }

    public Task<BaseResponse<PaginatedResult<Customer>>> SearchAsync(CustomerSearchFilter filter, int pageNumber, int pageSize)
    {
        LastSearchFilter = filter;
        LastSearchPage = pageNumber;
        LastSearchPageSize = pageSize;
        var items = Customers.OrderByDescending(c => c.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        var page = new PaginatedResult<Customer>(items, Customers.Count, pageNumber, pageSize,
            (int)Math.Ceiling(Customers.Count / (double)pageSize));
        return Task.FromResult(new BaseResponse<PaginatedResult<Customer>>(page, 200, "ok"));
    }
}
