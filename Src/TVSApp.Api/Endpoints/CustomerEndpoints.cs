using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.Exceptions;
using TVS_App.Api.SignalR;
using TVS_App.Application.Commands;
using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.Handlers;
using TVS_App.Domain.Entities;

namespace TVS_App.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        app.MapPost("/create-customer", async (CustomerHandler handler, CreateCustomerCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            try
            {
                command.Validate();

                var createResult = await handler.CreateCustomerAsync(command);
                if (!createResult.IsSuccess)
                    return Results.Ok(createResult);

                await hubContext.Clients.All.SendAsync("Atualizar", createResult.Message);

                return Results.Ok(createResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<Customer>(ex);
                return Results.Ok(response);
            }
        }).WithTags("Customer").RequireAuthorization();

        app.MapPut("/update-customer", async (CustomerHandler handler, UpdateCustomerCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            try
            {
                command.Validate();

                var createResult = await handler.UpdateCustomerAsync(command);
                if (!createResult.IsSuccess)
                    return Results.Ok(createResult);

                await hubContext.Clients.All.SendAsync("Atualizar", createResult.Message);

                return Results.Ok(createResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<Customer>(ex);
                return Results.Ok(response);
            }
        }).WithTags("Customer").RequireAuthorization();

        app.MapGet("/get-customer-by-id/{id}", async (CustomerHandler handler, [FromRoute] long id) =>
        {
            try
            {
                var command = new GetCustomerByIdCommand { Id = id };
                command.Validate();

                var createResult = await handler.GetCustomerByIdAsync(command);
                if (!createResult.IsSuccess)
                    return Results.Ok(createResult);

                return Results.Ok(createResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<Customer>(ex);
                return Results.Ok(response);
            }
        }).WithTags("Customer").RequireAuthorization();
        
        app.MapGet("/get-customer-by-name", async (CustomerHandler handler, string name) =>
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    return Results.Ok();
                
                var customers = await handler.GetCustomerByNameAsync(name);
                return Results.Ok(customers);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<List<Customer>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("Customer").RequireAuthorization();
        
        app.MapGet("/get-all-customers/{pageSize}/{pageNumber}", async (
            CustomerHandler handler,
            [FromRoute] int pageSize,
            [FromRoute] int pageNumber) =>
        {
            try
            {
                var command = new PaginationCommand{PageNumber = pageNumber,  PageSize = pageSize};
                command.Validate();

                var createResult = await handler.GetAllCustomersAsync(command);
                if (!createResult.IsSuccess)
                    return Results.Ok(createResult);

                return Results.Ok(createResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<Customer>(ex);
                return Results.Ok(response);
            }
        }).WithTags("Customer").RequireAuthorization();
    }
}