using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Domain.Repositories;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers;

public class ServiceOrderQueryHandler : IServiceOrderQueryHandler
{
    private readonly IServiceOrderQueryRepository _repository;

    public ServiceOrderQueryHandler(IServiceOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaseResponse<ServiceOrderDto>> GetServiceOrderById(long id)
    {
        if (id <= 0)
            return new BaseResponse<ServiceOrderDto>(null, 400, "ID da ordem de serviço inválido");
        
        var repositoryResult = await _repository.GetByIdAsync(id);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<ServiceOrderDto>(null, 500, repositoryResult.Message);
        
        var dto =  repositoryResult.Data.MapToServiceOrderDto();
        return new BaseResponse<ServiceOrderDto>(dto);
    }

    public Task<BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>> GetPendingEstimateServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetPendingEstimateAsync);

    public Task<BaseResponse<PaginatedResult<AwaitingResponseServiceOrder>>> GetAwaitingResponseServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetAwaitingResponseAsync);

    public Task<BaseResponse<PaginatedResult<ApprovedServiceOrder>>> GetApprovedServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetApprovedAsync);

    public Task<BaseResponse<PaginatedResult<RejectedServiceOrder>>> GetRejectedServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetRejectedAsync);

    public Task<BaseResponse<PaginatedResult<AwaitingPartServiceOrder>>> GetAwaitingPartServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetAwaitingPartAsync);

    public Task<BaseResponse<PaginatedResult<ReadyForPickupServiceOrder>>> GetReadyForPickupServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetReadyForPickupAsync);

    private static async Task<BaseResponse<PaginatedResult<T>>> GetPagedAsync<T>(
        int pageNumber,
        int pageSize,
        Func<int, int, Task<BaseResponse<PaginatedResult<T>>>> query)
    {
        if (!ValidatePagination(pageNumber, pageSize))
            return new BaseResponse<PaginatedResult<T>>(null, 400, "Paginação inválida");

        var repositoryResult = await query(pageNumber, pageSize);
        if (!repositoryResult.IsSuccess)
            return new BaseResponse<PaginatedResult<T>>(null, 500, repositoryResult.Message);

        return repositoryResult;
    }

    private static bool ValidatePagination(int pageNumber, int pageSize)
        => pageNumber >= 1 && pageSize >= 1;
}