using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.SignalR;
using TVS_App.Application.Handlers;

namespace TVS_App.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        app.MapPost("/notifications", async (NotificationHandler handler, string title, string message, IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.CreateNotification(title, message);
            
            await hubContext.Clients.All.SendAsync("Atualizar", result.Message);
            
            return Results.Ok(result);
        }).WithTags("Notifications");

        app.MapGet("/notifications/unread", async (NotificationHandler handler) =>
        {
            var result = await handler.GetUnreadNotifications();
            return Results.Ok(result);
        }).WithTags("Notifications");

        app.MapPut("/notifications/{id}/read", async (NotificationHandler handler, long id, IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.MarkNotificationAsRead(id);
            
            await hubContext.Clients.All.SendAsync("Atualizar", result.Message);
            
            return Results.Ok(result);
        }).WithTags("Notifications");
    }
}