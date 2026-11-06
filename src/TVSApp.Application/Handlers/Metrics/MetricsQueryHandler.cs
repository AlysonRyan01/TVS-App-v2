using TVS_App.Application.DTOs.Dashboard;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Metrics;

public class MetricsQueryHandler : IMetricsQueryHandler
{
    private readonly IMetricsService _metricsService;

    public MetricsQueryHandler(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public async Task<BaseResponse<DashboardMetricsDto>> GetDashboard(int? month, int? year, DateTime? from, DateTime? to)
    {
        if (from.HasValue != to.HasValue)
            return Bad("Informe as duas datas do período.");

        if (from.HasValue && to.HasValue)
        {
            var start = from.Value.Date;
            var end = to.Value.Date;
            if (end < start)
                return Bad("A data final não pode ser antes da inicial.");

            var byRange = await _metricsService.GetDashboardMetricsByRangeAsync(start, end);
            return Ok(byRange);
        }

        var m = month ?? DateTime.Today.Month;
        var y = year ?? DateTime.Today.Year;
        if (m is < 1 or > 12 || y is < 2000 or > 2100)
            return Bad("Mês ou ano inválido.");

        var byMonth = await _metricsService.GetDashboardMetricsAsync(m, y);
        return Ok(byMonth);
    }

    private static BaseResponse<DashboardMetricsDto> Ok(DashboardMetricsDto data)
        => new(data, 200, "Métricas carregadas com sucesso.");

    private static BaseResponse<DashboardMetricsDto> Bad(string message) => new(null, 400, message);
}
