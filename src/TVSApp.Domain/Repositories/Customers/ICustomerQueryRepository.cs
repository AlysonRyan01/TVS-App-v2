using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Domain.Repositories.Customers;

public interface ICustomerQueryRepository
{
    Task<BaseResponse<Customer?>> GetByIdAsync(long id);
    /// <summary>Clientes cujo nome contém o texto, os mais recentes primeiro, até <paramref name="limit"/>.</summary>
    Task<BaseResponse<List<Customer>>> GetCustomerByName(string name, int limit);
    Task<BaseResponse<PaginatedResult<Customer?>>> GetAllAsync(int pageNumber, int pageSize);
}