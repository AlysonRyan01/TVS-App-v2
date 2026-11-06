using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Repositories;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers;

public interface IServiceOrderQueryHandler
{
    Task<BaseResponse<ServiceOrderDto>> GetServiceOrderById(long id);
    Task<BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>> GetPendingEstimateServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<AwaitingResponseServiceOrder>>> GetAwaitingResponseServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ApprovedServiceOrder>>> GetApprovedServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<RejectedServiceOrder>>> GetRejectedServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<AwaitingPartServiceOrder>>> GetAwaitingPartServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ReadyForPickupServiceOrder>>> GetReadyForPickupServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ServiceOrderSearchItem>>> Search(ServiceOrderSearchQuery query);
}
