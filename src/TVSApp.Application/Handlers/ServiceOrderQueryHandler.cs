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
        => GetPagedAsync(pageNumber, pageSize, _repository.GetPendingEstimatesAsync);

    public Task<BaseResponse<PaginatedResult<WaitingResponseServiceOrder>>> GetWaitingResponseServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetWaitingResponseAsync);

    public Task<BaseResponse<PaginatedResult<ApprovedServiceOrder>>> GetApprovedServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetApprovedAsync);

    public Task<BaseResponse<PaginatedResult<RefusedServiceOrder>>> GetRefusedServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetRefusedAsync);

    public Task<BaseResponse<PaginatedResult<WaitingPartServiceOrder>>> GetWaitingPartServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetWaitingPartAsync);

    public Task<BaseResponse<PaginatedResult<ReadyServiceOrder>>> GetReadyServiceOrders(int pageNumber, int pageSize)
        => GetPagedAsync(pageNumber, pageSize, _repository.GetReadyAsync);

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