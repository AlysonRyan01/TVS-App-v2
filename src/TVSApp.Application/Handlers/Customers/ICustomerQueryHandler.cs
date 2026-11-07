using TVS_App.Application.DTOs;
using TVS_App.Application.Requests.CustomerRequests.CustomerQueries;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public interface ICustomerQueryHandler
{
    /// <summary>Barra "Buscar cliente": busca por nome no índice em memória (sem acento, mais recentes primeiro).</summary>
    Task<BaseResponse<List<CustomerSearchItemDto>>> SearchByName(string name);
    Task<BaseResponse<CustomerDto?>> GetById(long id);
    Task<BaseResponse<PaginatedResult<CustomerDto>>> Search(CustomerSearchQuery query);
}
