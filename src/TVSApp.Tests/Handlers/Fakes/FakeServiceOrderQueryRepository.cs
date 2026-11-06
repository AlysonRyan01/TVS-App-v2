using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Repositories;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVSApp.Tests.Handlers.Fakes;

public class FakeServiceOrderQueryRepository : IServiceOrderQueryRepository
{
    public ServiceOrder? ServiceOrderForUpdate { get; set; }
    public bool GetByIdForUpdateCalled { get; private set; }
    public string? LastQueueMethod { get; private set; }
    public (int PageNumber, int PageSize)? LastPagination { get; private set; }

    public Task<BaseResponse<ServiceOrder?>> GetByIdForUpdateAsync(long id)
    {
        GetByIdForUpdateCalled = true;
        var response = ServiceOrderForUpdate is null
            ? new BaseResponse<ServiceOrder?>(null, 404, $"A ordem de serviço com ID:{id} não existe")
            : new BaseResponse<ServiceOrder?>(ServiceOrderForUpdate);

        return Task.FromResult(response);
    }

    public Task<BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>> GetPendingEstimateAsync(int pageNumber, int pageSize)
        => Queue<PendingEstimateServiceOrder>(nameof(GetPendingEstimateAsync), pageNumber, pageSize);

    public Task<BaseResponse<PaginatedResult<AwaitingResponseServiceOrder>>> GetAwaitingResponseAsync(int pageNumber, int pageSize)
        => Queue<AwaitingResponseServiceOrder>(nameof(GetAwaitingResponseAsync), pageNumber, pageSize);

    public Task<BaseResponse<PaginatedResult<ApprovedServiceOrder>>> GetApprovedAsync(int pageNumber, int pageSize)
        => Queue<ApprovedServiceOrder>(nameof(GetApprovedAsync), pageNumber, pageSize);

    public Task<BaseResponse<PaginatedResult<RejectedServiceOrder>>> GetRejectedAsync(int pageNumber, int pageSize)
        => Queue<RejectedServiceOrder>(nameof(GetRejectedAsync), pageNumber, pageSize);

    public Task<BaseResponse<PaginatedResult<AwaitingPartServiceOrder>>> GetAwaitingPartAsync(int pageNumber, int pageSize)
        => Queue<AwaitingPartServiceOrder>(nameof(GetAwaitingPartAsync), pageNumber, pageSize);

    public Task<BaseResponse<PaginatedResult<ReadyForPickupServiceOrder>>> GetReadyForPickupAsync(int pageNumber, int pageSize)
        => Queue<ReadyForPickupServiceOrder>(nameof(GetReadyForPickupAsync), pageNumber, pageSize);

    private Task<BaseResponse<PaginatedResult<T>>> Queue<T>(string method, int pageNumber, int pageSize)
    {
        LastQueueMethod = method;
        LastPagination = (pageNumber, pageSize);
        var result = new PaginatedResult<T>(new List<T>(), 0, pageNumber, pageSize, null);
        return Task.FromResult(new BaseResponse<PaginatedResult<T>>(result));
    }

    public Task<BaseResponse<ServiceOrder?>> GetByIdAsync(long id)
        => Task.FromResult(ServiceOrderForUpdate is null
            ? new BaseResponse<ServiceOrder?>(null, 404, $"A ordem de serviço com ID:{id} não existe")
            : new BaseResponse<ServiceOrder?>(ServiceOrderForUpdate));

    public ServiceOrderSearchFilter? LastSearchFilter { get; private set; }
    public int SearchCalls { get; private set; }

    public BaseResponse<PaginatedResult<ServiceOrderSearchItem>>? SearchResponse { get; set; }

    public Task<BaseResponse<PaginatedResult<ServiceOrderSearchItem>>> SearchAsync(ServiceOrderSearchFilter filter, int pageNumber, int pageSize)
    {
        SearchCalls++;
        LastSearchFilter = filter;
        LastPagination = (pageNumber, pageSize);
        return Task.FromResult(SearchResponse ?? new BaseResponse<PaginatedResult<ServiceOrderSearchItem>>(
            new PaginatedResult<ServiceOrderSearchItem>(new List<ServiceOrderSearchItem>(), 0, pageNumber, pageSize, null)));
    }
}
