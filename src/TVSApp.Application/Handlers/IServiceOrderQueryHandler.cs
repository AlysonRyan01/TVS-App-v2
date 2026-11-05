using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Domain.Repositories;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers;

public interface IServiceOrderQueryHandler
{
    Task<BaseResponse<ServiceOrderDto>> GetServiceOrderById(long id);
    Task<BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>> GetPendingEstimateServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<WaitingResponseServiceOrder>>> GetWaitingResponseServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ApprovedServiceOrder>>> GetApprovedServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<RefusedServiceOrder>>> GetRefusedServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<WaitingPartServiceOrder>>> GetWaitingPartServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ReadyServiceOrder>>> GetReadyServiceOrders(int pageNumber, int pageSize);
}