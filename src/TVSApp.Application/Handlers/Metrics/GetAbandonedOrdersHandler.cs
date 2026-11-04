using MediatR;
using TVS_App.Application.DTOs.Dashboard;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.MetricsRequests;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Metrics;

public class GetAbandonedOrdersHandler : IRequestHandler<GetAbandonedOrdersQuery, BaseResponse<AbandonedOrdersDto>>
{
    private readonly IMetricsService _metricsService;

    public GetAbandonedOrdersHandler(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public async Task<BaseResponse<AbandonedOrdersDto>> Handle(
        GetAbandonedOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var data = await _metricsService.GetAbandonedOrdersAsync();
        return new BaseResponse<AbandonedOrdersDto>(data, 200, "Ordens abandonadas carregadas com sucesso.");
    }
}
