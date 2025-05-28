using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.Exceptions;
using TVS_App.Api.SignalR;
using TVS_App.Application.Commands;
using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.DTOs;
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
                command.Normalize();
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
                command.Normalize();
                command.Validate();

                var updateResult = await handler.UpdateCustomerAsync(command);
                if (!updateResult.IsSuccess)
                    return Results.Ok(updateResult);

                await hubContext.Clients.All.SendAsync("Atualizar", updateResult.Message);

                return Results.Ok(updateResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<CustomerDto>(ex);
                return Results.Ok(response);
            }
        }).WithTags("Customer").RequireAuthorization();

        app.MapGet("/get-customer-by-id/{id}", async (CustomerHandler handler, [FromRoute] long id) =>
        {
            try
            {
                var command = new GetCustomerByIdCommand { Id = id };
                command.Validate();

                var getResult = await handler.GetCustomerByIdAsync(command);
                if (!getResult.IsSuccess)
                    return Results.Ok(getResult);

                return Results.Ok(getResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<CustomerDto>(ex);
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
                var response = EndpointExceptions.Handle<List<CustomerDto>>(ex);
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

                var getAllResult = await handler.GetAllCustomersAsync(command);
                if (!getAllResult.IsSuccess)
                    return Results.Ok(getAllResult);

                return Results.Ok(getAllResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<CustomerDto>(ex);
                return Results.Ok(response);
            }
        }).WithTags("Customer").RequireAuthorization();
    }
}