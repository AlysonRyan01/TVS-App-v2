using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.Extensions;
using TVS_App.Api.SignalR;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.CustomerRequests.CustomerCommands;
using TVS_App.Application.Handlers.Customers;

namespace TVS_App.Api.Endpoints.Customers;

public static class CustomerCommandEndpoints
{
    public static void MapCustomerCommandEndpoints(this WebApplication app)
    {
        app.MapPost("/customers", async (
                ICustomerCommandHandler handler,
                CreateCustomerCommand command,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.Create(command);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithTags("Customer").RequireAuthorization();

        app.MapPut("/customers/{id:long}", async (
                ICustomerCommandHandler handler,
                long id,
                UpdateCustomerCommand command,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.Update(id, command);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithTags("Customer").RequireAuthorization();
    }
}
