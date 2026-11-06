using TVS_App.Application.DTOs;
using TVS_App.Application.Requests.CustomerRequests.CustomerQueries;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public interface ICustomerQueryHandler
{
    Task<BaseResponse<List<CustomerDto>>> SearchByName(string name);
    Task<BaseResponse<PaginatedResult<CustomerDto>>> Search(CustomerSearchQuery query);
}
