using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Domain.Repositories.ServiceOrders;

public interface IServiceOrderQueryRepository
{
    Task<BaseResponse<ServiceOrder?>> GetByIdAsync(long id);
    Task<BaseResponse<ServiceOrder?>> GetByIdForUpdateAsync(long id);
    Task<BaseResponse<PaginatedResult<ServiceOrder>>> GetAllAsync(int pageNumber, int pageSize);
    Task<BaseResponse<List<ServiceOrder>>> GetByCustomerNameAsync(string customerName);
    Task<BaseResponse<List<ServiceOrder>>> GetBySerialNumberAsync(string serialNumber);
    Task<BaseResponse<List<ServiceOrder>>> GetByModelAsync(string model);
    Task<BaseResponse<List<ServiceOrder>>> GetByEnterpriseAsync(EEnterprise enterprise);
    Task<BaseResponse<List<ServiceOrder>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>> GetPendingEstimatesAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<WaitingResponseServiceOrder>>> GetWaitingResponseAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ApprovedServiceOrder>>> GetApprovedAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<RefusedServiceOrder>>> GetRefusedAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<WaitingPartServiceOrder>>> GetWaitingPartAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ReadyServiceOrder>>> GetReadyAsync(int pageNumber, int pageSize);
}