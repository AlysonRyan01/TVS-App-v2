using MediatR;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.SignalR;
using TVS_App.Application.DTOs;
using TVS_App.Application.Requests.NotificationRequests.NotificationCommands;
using TVS_App.Application.Requests.NotificationRequests.NotificationQueries;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        app.MapPost("/notifications", async (
                ISender handler,
                CreateNotification dto,
                IHubContext<ServiceOrderHub> hubContext) => 
            {
                var request = new CreateNotificationCommand(dto.Title, dto.Message);
                var result = await handler.Send(request);
                
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);
                
                return Results.Ok(result);
            })
        .WithTags("Notifications")
        .WithName("CreateNotification")
        .WithSummary("Cria uma nova notificação.")
        .WithDescription("Recebe título e mensagem no corpo da requisição. Cria uma notificação e envia para todos os clientes conectados via SignalR.")
        .Produces<BaseResponse<Notification>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<Notification>>(StatusCodes.Status400BadRequest, "application/json");

        app.MapGet("/notifications/unread", async (ISender handler) =>
            {
                var request = new GetUnreadNotificationsQuery();
                var result = await handler.Send(request);
            
                return Results.Ok(result);
            })
        .WithTags("Notifications")
        .WithName("GetUnreadNotifications")
        .WithSummary("Busca todas as notificações não lidas.")
        .WithDescription("Retorna uma lista de notificações que ainda não foram marcadas como lidas pelo usuário.")
        .Produces<BaseResponse<IEnumerable<Notification>>>(StatusCodes.Status200OK, "application/json");

        app.MapPut("/notifications/{id}/read", async (
                ISender handler, long id,
                IHubContext<ServiceOrderHub> hubContext) =>
            {
                var request = new MarkNotificationAsReadCommand(id);
                var result = await handler.Send(request);
            
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);
            
                return Results.Ok(result);
            })
        .WithTags("Notifications")
        .WithName("MarkNotificationAsRead")
        .WithSummary("Marca uma notificação como lida.")
        .WithDescription("Recebe o ID da notificação via rota e atualiza seu status para 'lida'. Após a atualização, notifica todos os clientes conectados via SignalR.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json");
    }
}