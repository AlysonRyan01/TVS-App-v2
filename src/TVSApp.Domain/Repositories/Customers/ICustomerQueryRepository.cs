using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Domain.Repositories.Customers;

public interface ICustomerQueryRepository
{
    Task<BaseResponse<Customer?>> GetByIdAsync(long id);
    Task<BaseResponse<PaginatedResult<Customer>>> SearchAsync(CustomerSearchFilter filter, int pageNumber, int pageSize);
}