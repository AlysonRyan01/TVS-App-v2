using MediatR;
using TVS_App.Application.DTOs.Dashboard;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.MetricsRequests;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Metrics;

public class GetDashboardMetricsHandler : IRequestHandler<GetDashboardMetricsQuery, BaseResponse<DashboardMetricsDto>>
{
    private readonly IMetricsService _metricsService;

    public GetDashboardMetricsHandler(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public async Task<BaseResponse<DashboardMetricsDto>> Handle(
        GetDashboardMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var data = await _metricsService.GetDashboardMetricsAsync(request.Month, request.Year);
        return new BaseResponse<DashboardMetricsDto>(data, 200, "Métricas carregadas com sucesso.");
    }
}
