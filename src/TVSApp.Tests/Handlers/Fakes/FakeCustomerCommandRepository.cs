using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;

namespace TVSApp.Tests.Handlers.Fakes;

public class FakeCustomerCommandRepository : ICustomerCommandRepository
{
    public Customer? Created { get; private set; }
    public Customer? Updated { get; private set; }

    public Task<BaseResponse<Customer>> CreateAsync(Customer customer)
    {
        customer.Id = 10;
        Created = customer;
        return Task.FromResult(new BaseResponse<Customer>(customer));
    }

    public Task<BaseResponse<Customer?>> UpdateAsync(Customer customer)
    {
        Updated = customer;
        return Task.FromResult(new BaseResponse<Customer?>(customer));
    }
}
