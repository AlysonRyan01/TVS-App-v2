using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;

namespace TVSApp.Tests.Handlers.Fakes;

public class FakeCustomerCommandRepository : ICustomerCommandRepository
{
    public Customer? Created { get; private set; }
    public Customer? Updated { get; private set; }
    public Exception? ThrowOnWrite { get; set; }

    public Task<BaseResponse<Customer>> CreateAsync(Customer customer)
    {
        if (ThrowOnWrite is not null)
            throw ThrowOnWrite;
        customer.Id = 10;
        Created = customer;
        return Task.FromResult(new BaseResponse<Customer>(customer));
    }

    public Task<BaseResponse<Customer?>> UpdateAsync(Customer customer)
    {
        if (ThrowOnWrite is not null)
            throw ThrowOnWrite;
        Updated = customer;
        return Task.FromResult(new BaseResponse<Customer?>(customer));
    }
}
