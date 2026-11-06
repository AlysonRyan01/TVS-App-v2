using MediatR;
using TVS_App.Api.Extensions;
using TVS_App.Application.Handlers.Metrics;
using TVS_App.Application.Requests.MetricsRequests;

namespace TVS_App.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        // Tela "Métricas": ?month=&year= (padrão: mês atual) ou ?from=&to=.
        app.MapGet("/metrics", async (IMetricsQueryHandler handler, int? month, int? year, DateTime? from, DateTime? to) =>
        {
            var result = await handler.GetDashboard(month, year, from, to);
            return result.ToResult();
        }).WithTags("Dashboard").RequireAuthorization();

        app.MapGet("/get-abandoned-orders", async (ISender mediator) =>
        {
            var result = await mediator.Send(new GetAbandonedOrdersQuery());
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithTags("Dashboard")
        .WithName("GetAbandonedOrders")
        .WithSummary("Retorna ordens com reparo/sem conserto que não foram retiradas.")
        .RequireAuthorization();
    }
}
