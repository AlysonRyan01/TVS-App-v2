using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Repositories;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetRejectedEstimateServiceOrdersHandler : 
    IRequestHandler<GetRejectedEstimateServiceOrdersQuery, BaseResponse<PaginatedResult<RefusedServiceOrder>>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetRejectedEstimateServiceOrdersHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<PaginatedResult<RefusedServiceOrder>>> Handle(
        GetRejectedEstimateServiceOrdersQuery request, 
        CancellationToken cancellationToken = default)
    {
        request.Validate();
        
        var queryResult = await _serviceOrderQueryRepository
            .GetRejectedEstimateServiceOrders(request.PageNumber, request.PageSize);
        
        if (!queryResult.IsSuccess || queryResult.Data == null)
            return new BaseResponse<PaginatedResult<RefusedServiceOrder>>(null, queryResult.Code, queryResult.Message);

        var serviceOrders = queryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();
        
        var paginatedResult = new PaginatedResult<RefusedServiceOrder>(
            serviceOrders,
            queryResult.Data.TotalCount,
            queryResult.Data.PageNumber,
            queryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<RefusedServiceOrder>>(
            paginatedResult, 
            200, 
            "Ordens de serviço obtidas com sucesso!");
    }
}