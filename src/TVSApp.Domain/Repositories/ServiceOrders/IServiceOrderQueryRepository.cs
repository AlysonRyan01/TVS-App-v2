using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Domain.Repositories.ServiceOrders;

public interface IServiceOrderQueryRepository
{
    Task<BaseResponse<ServiceOrder?>> GetById(long id);
    Task<BaseResponse<ServiceOrder?>> GetByIdForUpdate(long id);
    Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetAllAsync(int pageNumber, int pageSize);
    Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByCustomerName(string customerName);
    Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersBySerialNumber(string serialNumber);
    Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByModel(string model);
    Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByEnterprise(EEnterprise enterprise);
    Task<BaseResponse<List<ServiceOrder>>> GetServiceOrdersByDate(DateTime startDate, DateTime endDate);
    Task<BaseResponse<PaginatedResult<PendingEstimateServiceOrder?>>> GetPendingEstimatesAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<WaitingResponseServiceOrder?>>> GetWaitingResponseAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ApprovedServiceOrder?>>> GetPendingPartPurchase(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<RefusedServiceOrder?>>> GetRejectedEstimateServiceOrders(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<WaitingPartServiceOrder?>>> GetWaitingPartsAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ReadyServiceOrder?>>> GetWaitingPickupAsync(int pageNumber, int pageSize);
    Task<BaseResponse<PaginatedResult<ServiceOrder?>>> GetDeliveredAsync(int pageNumber, int pageSize);
}