using TVS_App.Application.DTOs;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public interface ICustomerQueryHandler
{
    /// <summary>Busca por parte do nome (mínimo 3 letras); devolve até 21 clientes, os mais recentes primeiro.</summary>
    Task<BaseResponse<List<CustomerDto>>> SearchByName(string name);
}
