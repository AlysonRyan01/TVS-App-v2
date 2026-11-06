using TVS_App.Application.DTOs;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public interface ICustomerQueryHandler
{
    Task<BaseResponse<List<CustomerDto>>> SearchByName(string name);
}
