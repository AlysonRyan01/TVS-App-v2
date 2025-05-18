using TVS_App.Api.Endpoints;

namespace TVS_App.Api.Common;

public static class AppExtensions
{
    public static void AddAuthorization(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    public static void AddEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
        app.MapCustomerEndpoints();
        app.MapServiceOrderEndpoints();
        app.MapNotificationEndpoints();
    }

    public static void AddSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
}