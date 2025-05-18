using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.Exceptions;
using TVS_App.Api.SignalR;
using TVS_App.Application.Commands;
using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.DTOs;
using TVS_App.Application.Handlers;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;

namespace TVS_App.Api.Endpoints;

public static class ServiceOrderEndpoints
{
    public static void MapServiceOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/create-service-order", async (ServiceOrderHandler handler, CreateServiceOrderCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            try
            {
                command.Validate();

                var createOrderResult = await handler.CreateServiceOrderAndReturnPdfAsync(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

                return Results.File(createOrderResult.Data!, "application/pdf", "ordem_servico.pdf");
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/edit-service-order", async ([FromServices]ServiceOrderHandler handler, [FromBody]ServiceOrder serviceOrder, [FromServices]IHubContext<ServiceOrderHub> hubContext) =>
        {
            try
            {
                var createOrderResult = await handler.EditServiceOrderAsync(serviceOrder);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-service-order-by-id/{id}", async (ServiceOrderHandler handler, long id) =>
        {
            try
            {
                var command = new GetServiceOrderByIdCommand { Id = id };
                command.Validate();

                var createOrderResult = await handler.GetServiceOrderById(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-service-orders-by-customer-name", async (ServiceOrderHandler handler, [FromQuery] string name) =>
        {
            try
            {
                var createOrderResult = await handler.GetServiceOrdersByCustomerName(name);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<List<ServiceOrder>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-serial-number", async (ServiceOrderHandler handler, [FromQuery] string serialNumber) =>
        {
            try
            {
                var createOrderResult = await handler
                    .GetServiceOrdersBySerialNumberAsync(
                        new GetServiceOrdersBySerialNumberCommand { SerialNumber = serialNumber });
                
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<List<ServiceOrder>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-model", async (ServiceOrderHandler handler, [FromQuery] string model) =>
        {
            try
            {
                var createOrderResult = await handler
                    .GetServiceOrdersByModelAsync(
                        new GetServiceOrdersByModelCommand() { Model = model });
                
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<List<ServiceOrder>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-enterprise", async (ServiceOrderHandler handler, [FromQuery] EEnterprise enterprise) =>
        {
            try
            {
                var createOrderResult = await handler
                    .GetServiceOrdersByEnterpriseAsync(enterprise);
                
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<List<ServiceOrder>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-date", async (ServiceOrderHandler handler, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate) =>
        {
            try
            {
                var createOrderResult = await handler
                    .GetServiceOrdersByDateAsync(startDate, endDate);
                
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<List<ServiceOrder>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-service-order-for-customer/{id}/{code}", async (ServiceOrderHandler handler, long id, string code) =>
        {
            try
            {
                var command = new GetServiceOrdersForCustomerCommand { ServiceOrderId = id, SecurityCode = code };
                command.Validate();

                var createOrderResult = await handler.GetServiceOrderForCustomer(command);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder");

        app.MapGet("/get-all-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            try
            {
                var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
                command.Validate();

                var createOrderResult = await handler.GetAllServiceOrdersAsync(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<PaginatedResult<ServiceOrder?>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-pending-estimates-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            try
            {
                var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
                command.Validate();

                var createOrderResult = await handler.GetPendingEstimatesAsync(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<PaginatedResult<ServiceOrder?>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-waiting-response-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            try
            {
                var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
                command.Validate();

                var createOrderResult = await handler.GetWaitingResponseAsync(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<PaginatedResult<ServiceOrder?>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-pending-purchase-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            try
            {
                var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
                command.Validate();

                var createOrderResult = await handler.GetPendingPurchasePartAsync(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<PaginatedResult<ServiceOrder?>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-waiting-parts-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            try
            {
                var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
                command.Validate();

                var createOrderResult = await handler.GetWaitingPartsAsync(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<PaginatedResult<ServiceOrder?>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-waiting-pickup-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            try
            {
                var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
                command.Validate();

                var createOrderResult = await handler.GetWaitingPickupAsync(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<PaginatedResult<ServiceOrder?>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-delivered-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            try
            {
                var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
                command.Validate();

                var createOrderResult = await handler.GetDeliveredAsync(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<PaginatedResult<ServiceOrder?>>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-product-location", async (ServiceOrderHandler handler, AddProductLocationCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            try
            {
                command.Validate();

                var addLocationResult = await handler.AddProductLocation(command);
                if (!addLocationResult.IsSuccess)
                    return Results.Ok(addLocationResult);

                await hubContext.Clients.All.SendAsync("Atualizar", addLocationResult.Message);

                return Results.Ok(addLocationResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-service-order-estimate", async (ServiceOrderHandler handler, AddServiceOrderEstimateCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            try
            {
                command.Validate();

                var createOrderResult = await handler.AddServiceOrderEstimate(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-service-order-approve-estimate", async (ServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            try
            {
                command.Validate();

                var createOrderResult = await handler.AddServiceOrderApproveEstimate(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder");

        app.MapPut("/add-service-order-reject-estimate", async (ServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            try
            {
                command.Validate();

                var createOrderResult = await handler.AddServiceOrderRejectEstimate(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder");

        app.MapPut("/add-service-order-purchased-part", async (ServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            try
            {
                command.Validate();

                var createOrderResult = await handler.AddPurchasedPart(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-service-order-repair", async (ServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            try
            {
                command.Validate();

                var createOrderResult = await handler.AddServiceOrderRepair(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

                return Results.Ok(createOrderResult);
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-service-order-delivery", async (ServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            try
            {
                command.Validate();

                var createOrderResult = await handler.AddServiceOrderDeliveryAndReturnPdfAsync(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

                if (createOrderResult.Data != null)
                    return Results.File(createOrderResult.Data, "application/pdf", "ordem_servico.pdf");
                
                return Results.Ok();
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/regenerate-service-order-pdf", async (ServiceOrderHandler handler, GetServiceOrderByIdCommand command) =>
        {
            try
            {
                command.Validate();

                var createOrderResult = await handler.RegenerateAndReturnPdfAsync(command);
                if (!createOrderResult.IsSuccess)
                    return Results.Ok(createOrderResult);

                return Results.File(createOrderResult.Data!, "application/pdf", "ordem_servico.pdf");
            }
            catch (Exception ex)
            {
                var response = EndpointExceptions.Handle<ServiceOrder>(ex);
                return Results.Ok(response);
            }
        }).WithTags("ServiceOrder").RequireAuthorization();
    }
}