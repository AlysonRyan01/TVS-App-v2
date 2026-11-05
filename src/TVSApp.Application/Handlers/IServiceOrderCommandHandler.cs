using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers;

public interface IServiceOrderCommandHandler
{
    Task<BaseResponse<ServiceOrderDto?>> AddEstimate(AddServiceOrderEstimateCommand command);
    Task<BaseResponse<ServiceOrderDto?>> ApproveEstimate(long id);
    Task<BaseResponse<ServiceOrderDto?>> RejectEstimate(long id);
    Task<BaseResponse<ServiceOrderDto?>> RegisterPartPurchase(long id);
    Task<BaseResponse<ServiceOrderDto?>> MarkRejectedEstimateAsReadyForPickup(long id);
    Task<BaseResponse<ServiceOrderDto?>> RegisterRepair(long id);
    Task<BaseResponse<byte[]>> RegisterDelivery(long id);
    Task<BaseResponse<string?>> AddProductLocation(AddProductLocationCommand command);
}
