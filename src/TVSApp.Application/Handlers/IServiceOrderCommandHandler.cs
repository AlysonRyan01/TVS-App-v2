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
    Task<BaseResponse<CreatedServiceOrderPdf>> CreateServiceOrder(CreateServiceOrderCommand command);
    Task<BaseResponse<CreatedServiceOrderPdf>> CreateSalesServiceOrder(CreateSalesServiceOrderCommand command);
    Task<BaseResponse<byte[]>> RegeneratePdf(long id);
    Task<BaseResponse<ServiceOrderDto?>> UpdatePartPurchaseCost(long id, decimal partPurchaseCost);
    Task<BaseResponse<ServiceOrderDto?>> UpdateServiceOrder(long id, EditServiceOrderCommand command);
    Task<BaseResponse<ServiceOrderDto?>> ApproveEstimateByCustomer(long id, string code);
    Task<BaseResponse<ServiceOrderDto?>> RejectEstimateByCustomer(long id, string code);
    Task<BaseResponse<string?>> AddPayment(long serviceOrderId, AddPaymentCommand command);
    Task<BaseResponse<string?>> UpdatePayment(long serviceOrderId, long paymentId, UpdatePaymentCommand command);
    Task<BaseResponse<string?>> RemovePayment(long serviceOrderId, long paymentId);
    Task<BaseResponse<byte[]>> GetPaymentReceipt(long serviceOrderId, long paymentId);
}
