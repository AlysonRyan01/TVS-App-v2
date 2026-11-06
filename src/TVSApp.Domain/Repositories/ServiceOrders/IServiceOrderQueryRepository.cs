using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Domain.Repositories.ServiceOrders;

public interface IServiceOrderQueryRepository
{
    Task<BaseResponse<ServiceOrder?>> GetByIdAsync(long id);
    Task<BaseResponse<ServiceOrder?>> GetByIdForUpdateAsync(long id);
    Task<BaseResponse<List<ServiceOrder>>> GetByEnterpriseAsync(EEnterprise enterprise);
    Task<BaseResponse<PaginatedResult<ServiceOrderSearchItem>>> SearchAsync(ServiceOrderSearchFilter filter, int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>> GetPendingEstimateAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<AwaitingResponseServiceOrder>>> GetAwaitingResponseAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ApprovedServiceOrder>>> GetApprovedAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<RejectedServiceOrder>>> GetRejectedAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<AwaitingPartServiceOrder>>> GetAwaitingPartAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ReadyForPickupServiceOrder>>> GetReadyForPickupAsync(int pageNumber, int pageSize);
}