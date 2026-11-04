using MediatR;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Repositories;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetPendingPurchasePartHandler 
    : IRequestHandler<GetPendingPurchasePartQuery, BaseResponse<PaginatedResult<ApprovedServiceOrder>>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetPendingPurchasePartHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<PaginatedResult<ApprovedServiceOrder>>> Handle(
        GetPendingPurchasePartQuery request, 
        CancellationToken cancellationToken = default)
    {
        request.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetPendingPartPurchase(
            request.PageNumber, 
            request.PageSize);
        
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<PaginatedResult<ApprovedServiceOrder>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var paginatedResult = new PaginatedResult<ApprovedServiceOrder>(
            serviceOrders,
            repositoryResult.Data.TotalCount,
            repositoryResult.Data.PageNumber,
            repositoryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<ApprovedServiceOrder>>(
            paginatedResult, 
            200, 
            "Ordens de serviço obtidas com sucesso!");
    }
}