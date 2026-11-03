using MediatR;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Repositories;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetPendingEstimatesHandler 
    : IRequestHandler<GetPendingEstimatesQuery, BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetPendingEstimatesHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>> Handle(
        GetPendingEstimatesQuery request, 
        CancellationToken cancellationToken = default)
    {
        request.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetPendingEstimatesAsync(
            request.PageNumber, 
            request.PageSize);
        
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var paginatedResult = new PaginatedResult<PendingEstimateServiceOrder>(
            serviceOrders,
            repositoryResult.Data.TotalCount,
            repositoryResult.Data.PageNumber,
            repositoryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<PendingEstimateServiceOrder>>(
            paginatedResult, 
            200, 
            "Ordens de serviço obtidas com sucesso!");
    }
}