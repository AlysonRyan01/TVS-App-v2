using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TVS_App.Api.Endpoints.Auth;
using TVS_App.Api.Endpoints.Customers;
using TVS_App.Api.Endpoints.Internal;
using TVS_App.Api.Endpoints.Metrics;
using TVS_App.Api.Endpoints.Public;
using TVS_App.Api.Endpoints.ServiceOrders;
using TVS_App.Api.Middlewares;
using TVS_App.Api.SignalR;
using TVS_App.Infrastructure.Data;
using TVS_App.Infrastructure.Models;

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
        app.MapAuthCommandEndpoints();
        app.MapCustomerQueryEndpoints();
        app.MapCustomerCommandEndpoints();
        app.MapServiceOrderQueryEndpoints();
        app.MapServiceOrderCommandEndpoints();
        app.MapMetricsQueryEndpoints();
        app.MapPublicQueryEndpoints();
        app.MapPublicCommandEndpoints();
        app.MapInternalWhatsAppEndpoints();
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

    public static async Task AddAdminUserAsync(this WebApplication app)
    {
        var email = app.Configuration["Admin:Email"];
        var password = app.Configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var result = await userManager.CreateAsync(new User { UserName = email, Email = email }, password);
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminUser");
        if (result.Succeeded)
            logger.LogInformation("Usuário inicial {Email} criado.", email);
        else
            logger.LogError("Não foi possível criar o usuário inicial {Email}: {Errors}", email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public static void AddExceptionMiddleware(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
    }
}