using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.SignalR;
using TVS_App.Application.DTOs;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.CustomerRequests.CustomerCommands;
using TVS_App.Application.Requests.CustomerRequests.CustomersQueries;
using TVS_App.Application.Handlers.Customers;
using TVS_App.Api.Extensions;
using TVS_App.Domain.Shared;

namespace TVS_App.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        // Rotas REST da tela "Adicionar OS" (feature 009).
        app.MapGet("/customers", async (ICustomerQueryHandler handler, [FromQuery] string name) =>
        {
            var result = await handler.SearchByName(name);
            return result.ToResult();
        }).WithTags("Customer").RequireAuthorization();

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

        app.MapGet("/addresses/{cep}", async (IAddressService addressService, string cep) =>
        {
            var result = await addressService.GetAddressByCep(cep);
            return result.ToResult();
        }).WithTags("Customer").RequireAuthorization();

        app.MapGet("/get-customer-by-id/{id}", async (
                ISender handler, 
                [FromRoute] long id) =>
            {
                var command = new GetCustomerByIdQuery(id);
                command.Validate();

                var getResult = await handler.Send(command);
            
                return Results.Ok(getResult);
            })
        .WithTags("Customer")
        .WithName("GetCustomerById")
        .WithSummary("Busca os dados de um cliente por ID.")
        .WithDescription(
            "Recebe o ID do cliente via rota, cria um comando de consulta, valida o comando e retorna os dados do cliente. "
            + "Caso não encontre, retorna uma resposta genérica informando o motivo.")
        .Produces<BaseResponse<CustomerDto>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
        
        app.MapGet("/get-all-customers/{pageSize}/{pageNumber}", async (
                ISender handler,
                [FromRoute] int pageSize,
                [FromRoute] int pageNumber) =>
            {
                var command = new GetAllCustomersQuery(pageNumber, pageSize);
                command.Validate();

                var getAllResult = await handler.Send(command);

                return Results.Ok(getAllResult);
            })
        .WithTags("Customer")
        .WithName("GetAllCustomers")
        .WithSummary("Busca todos os clientes com paginação.")
        .WithDescription("Recebe o número da página e a quantidade de itens por página na URL. "
                         + "Valida os dados e retorna uma lista paginada de clientes.")
        .Produces<BaseResponse<PaginatedResult<CustomerDto>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<PaginatedResult<CustomerDto>>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
    }
}