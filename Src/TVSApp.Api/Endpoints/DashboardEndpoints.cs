using MediatR;
using TVS_App.Application.Requests.MetricsRequests;

namespace TVS_App.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/get-dashboard-metrics", async (ISender mediator) =>
        {
            var result = await mediator.Send(new GetDashboardMetricsQuery());
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithTags("Dashboard")
        .WithName("GetDashboardMetrics")
        .WithSummary("Retorna todas as métricas do dashboard.")
        .RequireAuthorization();
    }
}
