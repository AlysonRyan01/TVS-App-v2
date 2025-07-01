using TVS_App.Application.Commands;
using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.DTOs;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Interfaces.Handlers;

public interface IServiceOrderHandler
{
    Task<BaseResponse<byte[]>> CreateServiceOrderAndReturnPdfAsync(CreateServiceOrderCommand command);
    Task<BaseResponse<byte[]>> CreateSalesServiceOrderAsync(CreateSalesServiceOrderCommand command);
    Task<BaseResponse<UpdateServiceOrderResponseDto?>> UpdateServiceOrderAsync(UpdateServiceOrderCommand command);
    Task<BaseResponse<UpdateServiceOrderResponseDto?>> EditServiceOrderAsync(EditServiceOrderCommand command);
    Task<BaseResponse<string?>> AddProductLocation(AddProductLocationCommand command);
    Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByCustomerName(string customerName);
    Task<BaseResponse<ServiceOrderDto?>> GetServiceOrderById(GetServiceOrderByIdCommand command);
    Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersBySerialNumberAsync(
        GetServiceOrdersBySerialNumberCommand command);
    Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByModelAsync(GetServiceOrdersByModelCommand command);
    Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByEnterpriseAsync(EEnterprise enterprise);
    Task<BaseResponse<List<ServiceOrderDto>>> GetServiceOrdersByDateAsync(DateTime startDate, DateTime endDate);
    Task<BaseResponse<EstimateServiceOrder>> GetServiceOrderForCustomer(GetServiceOrdersForCustomerCommand command);
    Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetAllServiceOrdersAsync(PaginationCommand command);
    Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetPendingEstimatesAsync(PaginationCommand command);
    Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetWaitingResponseAsync(PaginationCommand command);
    Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetPendingPurchasePartAsync(PaginationCommand command);
    Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetWaitingPartsAsync(PaginationCommand command);
    Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetWaitingPickupAsync(PaginationCommand command);
    Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> GetDeliveredAsync(PaginationCommand command);
    Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderEstimate(AddServiceOrderEstimateCommand command);
    Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderApproveEstimate(GetServiceOrderByIdCommand command);
    Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderRejectEstimate(GetServiceOrderByIdCommand command);
    Task<BaseResponse<ServiceOrderDto?>> AddPurchasedPart(GetServiceOrderByIdCommand command);
    Task<BaseResponse<ServiceOrderDto?>> AddServiceOrderRepair(GetServiceOrderByIdCommand command);
    Task<BaseResponse<byte[]>> AddServiceOrderDeliveryAndReturnPdfAsync(GetServiceOrderByIdCommand command);
    Task<BaseResponse<byte[]>> RegenerateAndReturnPdfAsync(GetServiceOrderByIdCommand command);
}