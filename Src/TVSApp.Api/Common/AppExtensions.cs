using Microsoft.EntityFrameworkCore;
using TVS_App.Api.Endpoints;
using TVS_App.Api.Middlewares;
using TVS_App.Api.SignalR;
using TVS_App.Infrastructure.Data;

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
    
    public static void AddSignalR(this WebApplication app)
    {
        app.MapHub<ServiceOrderHub>("/osHub");
    }

    public static void AddSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }

    public static void AddMigrations(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDataContext>();
            db.Database.Migrate();
        }
    }

    public static void AddExceptionMiddleware(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
    }
}