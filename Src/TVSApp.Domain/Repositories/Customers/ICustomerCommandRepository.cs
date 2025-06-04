using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Domain.Repositories.Customers;

public interface ICustomerCommandRepository
{
    Task<BaseResponse<Customer>> CreateAsync(Customer customer);
    Task<BaseResponse<Customer?>> UpdateAsync(Customer customer);
}