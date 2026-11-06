using TVS_App.Api.Extensions;
using TVS_App.Application.Handlers.Metrics;

namespace TVS_App.Api.Endpoints.Metrics;

public static class MetricsQueryEndpoints
{
    public static void MapMetricsQueryEndpoints(this WebApplication app)
    {
        app.MapGet("/metrics", async (IMetricsQueryHandler handler, int? month, int? year, DateTime? from, DateTime? to) =>
        {
            var result = await handler.GetDashboard(month, year, from, to);
            return result.ToResult();
        }).WithTags("Dashboard").RequireAuthorization();
    }
}
