using MediatR;
using TVS_App.Application.Requests.MetricsRequests;

namespace TVS_App.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/get-dashboard-metrics", async (ISender mediator, int? month, int? year) =>
        {
            var now = DateTime.UtcNow;
            var m = month ?? now.Month;
            var y = year ?? now.Year;
            var result = await mediator.Send(new GetDashboardMetricsQuery(m, y));
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithTags("Dashboard")
        .WithName("GetDashboardMetrics")
        .WithSummary("Retorna métricas do dashboard filtradas por mês/ano.")
        .RequireAuthorization();

        app.MapGet("/get-dashboard-metrics-by-range", async (ISender mediator, DateTime startDate, DateTime endDate) =>
        {
            var result = await mediator.Send(new GetDashboardMetricsByRangeQuery(startDate, endDate));
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithTags("Dashboard")
        .WithName("GetDashboardMetricsByRange")
        .WithSummary("Retorna métricas do dashboard filtradas por intervalo de datas.")
        .RequireAuthorization();
    }
}
