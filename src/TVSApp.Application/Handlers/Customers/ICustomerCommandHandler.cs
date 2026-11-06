using TVS_App.Application.DTOs;
using TVS_App.Application.Requests.CustomerRequests.CustomerCommands;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public interface ICustomerCommandHandler
{
    Task<BaseResponse<CustomerDto?>> Create(CreateCustomerCommand command);
    Task<BaseResponse<CustomerDto?>> Update(long id, UpdateCustomerCommand command);
}
