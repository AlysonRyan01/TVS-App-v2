using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.Extensions;
using TVS_App.Api.SignalR;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.CustomerRequests.CustomerCommands;
using TVS_App.Application.Handlers.Customers;

namespace TVS_App.Api.Endpoints.Customers;

public static class CustomerQueryEndpoints
{
    public static void MapCustomerQueryEndpoints(this WebApplication app)
    {
        app.MapGet("/customers", async (ICustomerQueryHandler handler, [FromQuery] string name) =>
        {
            var result = await handler.SearchByName(name);
            return result.ToResult();
        }).WithTags("Customer").RequireAuthorization();

        app.MapGet("/addresses/{cep}", async (IAddressService addressService, string cep) =>
        {
            var result = await addressService.GetAddressByCep(cep);
            return result.ToResult();
        }).WithTags("Customer").RequireAuthorization();
    }
}
