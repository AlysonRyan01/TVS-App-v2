using TVS_App.Application.Commands;
using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.DTOs;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Interfaces.Handlers;

public interface ICustomerHandler
{
    Task<BaseResponse<CustomerDto?>> CreateCustomerAsync(CreateCustomerCommand command);
    Task<BaseResponse<CustomerDto?>> UpdateCustomerAsync(UpdateCustomerCommand command);
    Task<BaseResponse<CustomerDto?>> GetCustomerByIdAsync(GetCustomerByIdCommand command);
    Task<BaseResponse<List<CustomerDto>>> GetCustomerByNameAsync(string name);
    Task<BaseResponse<PaginatedResult<CustomerDto?>>> GetAllCustomersAsync(PaginationCommand command);
}