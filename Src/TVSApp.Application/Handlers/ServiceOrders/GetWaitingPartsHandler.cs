using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Repositories;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetWaitingPartsHandler 
    : IRequestHandler<GetWaitingPartsQuery, BaseResponse<PaginatedResult<WaitingPartServiceOrder>>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetWaitingPartsHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<PaginatedResult<WaitingPartServiceOrder>>> Handle(
        GetWaitingPartsQuery request, 
        CancellationToken cancellationToken = default)
    {
        request.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetWaitingPartsAsync(
            request.PageNumber, 
            request.PageSize);
        
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<PaginatedResult<WaitingPartServiceOrder>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();
        
        var paginatedResult = new PaginatedResult<WaitingPartServiceOrder>(
            serviceOrders,
            repositoryResult.Data.TotalCount,
            repositoryResult.Data.PageNumber,
            repositoryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<WaitingPartServiceOrder>>(
            paginatedResult, 
            200, 
            "Ordens de serviço obtidas com sucesso!");
    }
}