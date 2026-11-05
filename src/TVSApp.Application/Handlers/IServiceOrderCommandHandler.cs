using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers;

public interface IServiceOrderCommandHandler
{
    Task<BaseResponse<ServiceOrderDto?>> AddEstimate(AddServiceOrderEstimateCommand command);
}
