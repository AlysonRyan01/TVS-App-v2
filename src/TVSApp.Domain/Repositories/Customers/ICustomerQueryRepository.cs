using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Domain.Repositories.Customers;

public interface ICustomerQueryRepository
{
    Task<BaseResponse<Customer?>> GetByIdAsync(long id);
    Task<BaseResponse<List<Customer>>> GetCustomerByName(string name, int limit);
}