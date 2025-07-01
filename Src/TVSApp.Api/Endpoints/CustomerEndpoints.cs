using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.SignalR;
using TVS_App.Application.Commands;
using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.DTOs;
using TVS_App.Application.Interfaces.Handlers;
using TVS_App.Domain.Shared;

namespace TVS_App.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        app.MapPost("/create-customer", async (ICustomerHandler handler, CreateCustomerCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Normalize();
            command.Validate();

            var createResult = await handler.CreateCustomerAsync(command);
            if (!createResult.IsSuccess)
                return Results.Ok(createResult);

            await hubContext.Clients.All.SendAsync("Atualizar", createResult.Message);

            return Results.Ok(createResult);
        })
        .WithTags("Customer")
        .WithName("CreateCustomer")
        .WithSummary("Cria um novo cliente no sistema.")
        .WithDescription("Recebe os dados do cliente via comando (CreateCustomerCommand), " + "valida, normaliza e envia para o handler.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapPut("/update-customer", async (ICustomerHandler handler, UpdateCustomerCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Normalize();
            command.Validate();

            var updateResult = await handler.UpdateCustomerAsync(command);
            if (!updateResult.IsSuccess)
                return Results.Ok(updateResult);

            await hubContext.Clients.All.SendAsync("Atualizar", updateResult.Message);

            return Results.Ok(updateResult);
        })
        .WithTags("Customer")
        .WithName("UpdateCustomer")
        .WithSummary("Atualiza os dados de um cliente existente.")
        .WithDescription("Recebe um comando do tipo UpdateCustomerCommand, valida e normaliza os dados")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapGet("/get-customer-by-id/{id}", async (ICustomerHandler handler, [FromRoute] long id) =>
        {
            var command = new GetCustomerByIdCommand { Id = id };
            command.Validate();

            var getResult = await handler.GetCustomerByIdAsync(command);
            if (!getResult.IsSuccess)
                return Results.Ok(getResult);

            return Results.Ok(getResult);
        })
        .WithTags("Customer")
        .WithName("GetCustomerById")
        .WithSummary("Busca os dados de um cliente por ID.")
        .WithDescription("Recebe o ID do cliente via rota, cria um comando de consulta, valida o comando e retorna os dados do cliente. "
                         + "Caso não encontre, retorna uma resposta genérica informando o motivo.")
        .Produces<BaseResponse<CustomerDto>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
        app.MapGet("/get-customer-by-name", async (ICustomerHandler handler, string name) =>
        {
            if (string.IsNullOrEmpty(name))
                return Results.Ok();
                
            var customers = await handler.GetCustomerByNameAsync(name);
            return Results.Ok(customers);
        })
        .WithTags("Customer")
        .WithName("GetCustomerByName")
        .WithSummary("Busca clientes pelo nome.")
        .WithDescription("Recebe o nome como parâmetro de query string, e retorna uma lista de clientes com nomes semelhantes. "
                         + "Caso o nome não seja informado, retorna uma resposta vazia.")
        .Produces<IEnumerable<CustomerDto>>(StatusCodes.Status200OK, "application/json")
        .RequireAuthorization();
        
        app.MapGet("/get-all-customers/{pageSize}/{pageNumber}", async (
                ICustomerHandler handler,
                [FromRoute] int pageSize,
                [FromRoute] int pageNumber) =>
        {
            var command = new PaginationCommand{PageNumber = pageNumber,  PageSize = pageSize};
            command.Validate();

            var getAllResult = await handler.GetAllCustomersAsync(command);
            if (!getAllResult.IsSuccess)
                return Results.Ok(getAllResult);

            return Results.Ok(getAllResult);
        })
        .WithTags("Customer")
        .WithName("GetAllCustomers")
        .WithSummary("Busca todos os clientes com paginação.")
        .WithDescription("Recebe o número da página e a quantidade de itens por página na URL. "
                         + "Valida os dados e retorna uma lista paginada de clientes.")
        .Produces<BaseResponse<IEnumerable<CustomerDto>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
    }
}