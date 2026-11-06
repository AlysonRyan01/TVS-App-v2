using TVS_App.Api.Extensions;
using TVS_App.Application.Handlers;

namespace TVS_App.Api.Endpoints.Public;

public static class PublicQueryEndpoints
{
    public static void MapPublicQueryEndpoints(this WebApplication app)
    {
        app.MapGet("/get-service-order-for-customer/{id:long}/{code}", async (
                IServiceOrderQueryHandler handler,
                long id,
                string code) =>
        {
            var result = await handler.GetForCustomer(id, code);
            return result.ToResult();
        }).WithTags("Public");
    }
}
