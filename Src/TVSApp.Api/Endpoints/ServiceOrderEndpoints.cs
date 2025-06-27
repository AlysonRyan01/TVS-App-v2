using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.SignalR;
using TVS_App.Application.Commands;
using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.Handlers;
using TVS_App.Domain.Enums;

namespace TVS_App.Api.Endpoints;

public static class ServiceOrderEndpoints
{
    public static void MapServiceOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/create-service-order", async (ServiceOrderHandler handler, CreateServiceOrderCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var createOrderResult = await handler.CreateServiceOrderAndReturnPdfAsync(command);
            if (!createOrderResult.IsSuccess)
                return Results.Ok(createOrderResult);

            await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

            return Results.File(createOrderResult.Data!, "application/pdf", "ordem_servico.pdf");
        }).WithTags("ServiceOrder").RequireAuthorization();
        
        app.MapPost("/create-sales-service-order", async (ServiceOrderHandler handler, CreateSalesServiceOrderCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var createOrderResult = await handler.CreateSalesServiceOrderAsync(command);
            if (!createOrderResult.IsSuccess)
                return Results.Ok(createOrderResult);

            await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

            return Results.File(createOrderResult.Data!, "application/pdf", "ordem_servico.pdf");
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/edit-service-order", async ([FromServices]ServiceOrderHandler handler, [FromBody]EditServiceOrderCommand command, [FromServices]IHubContext<ServiceOrderHub> hubContext) =>
        {
            var editOrderResult = await handler.EditServiceOrderAsync(command);
            if (!editOrderResult.IsSuccess)
                return Results.Ok(editOrderResult);

            await hubContext.Clients.All.SendAsync("Atualizar", editOrderResult.Message);

            return Results.Ok(editOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-service-order-by-id/{id}", async (ServiceOrderHandler handler, long id) =>
        {
            var command = new GetServiceOrderByIdCommand { Id = id };
            command.Validate();

            var getOrderResult = await handler.GetServiceOrderById(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-service-orders-by-customer-name", async (ServiceOrderHandler handler, [FromQuery] string name) =>
        {
            var getOrderResult = await handler.GetServiceOrdersByCustomerName(name);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-serial-number", async (ServiceOrderHandler handler, [FromQuery] string serialNumber) =>
        {
            var getOrderResult = await handler
                .GetServiceOrdersBySerialNumberAsync(
                    new GetServiceOrdersBySerialNumberCommand { SerialNumber = serialNumber });
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-model", async (ServiceOrderHandler handler, [FromQuery] string model) =>
        {
            var getOrderResult = await handler
                .GetServiceOrdersByModelAsync(
                    new GetServiceOrdersByModelCommand() { Model = model });
                
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-enterprise", async (ServiceOrderHandler handler, [FromQuery] EEnterprise enterprise) =>
        {
            var getOrderResult = await handler
                .GetServiceOrdersByEnterpriseAsync(enterprise);
                
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-date", async (ServiceOrderHandler handler, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate) =>
        {
            var getOrderResult = await handler
                .GetServiceOrdersByDateAsync(startDate, endDate);
                
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-service-order-for-customer/{id}/{code}", async (ServiceOrderHandler handler, long id, string code) =>
        {
            var command = new GetServiceOrdersForCustomerCommand { ServiceOrderId = id, SecurityCode = code };
            command.Validate();

            var getOrderResult = await handler.GetServiceOrderForCustomer(command);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder");

        app.MapGet("/get-all-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetAllServiceOrdersAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-pending-estimates-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetPendingEstimatesAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-waiting-response-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetWaitingResponseAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-pending-purchase-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetPendingPurchasePartAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-waiting-parts-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetWaitingPartsAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-waiting-pickup-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetWaitingPickupAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-delivered-service-orders/{pageNumber}/{pageSize}", async (ServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetDeliveredAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-product-location", async (ServiceOrderHandler handler, AddProductLocationCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addLocationResult = await handler.AddProductLocation(command);
            if (!addLocationResult.IsSuccess)
                return Results.Ok(addLocationResult);

            await hubContext.Clients.All.SendAsync("Atualizar", addLocationResult.Message);

            return Results.Ok(addLocationResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-service-order-estimate", async (ServiceOrderHandler handler,NotificationHandler notificationhandler,AddServiceOrderEstimateCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderEstimateResult = await handler.AddServiceOrderEstimate(command);
            if (!addServiceOrderEstimateResult.IsSuccess)
                return Results.Ok(addServiceOrderEstimateResult);

            await notificationhandler.CreateNotification("Orçamento Concluído", $"Favor enviar mensagem de orçamento para o responsável da ordem de serviço: {command.ServiceOrderId}");

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderEstimateResult.Message);

            return Results.Ok(addServiceOrderEstimateResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-service-order-approve-estimate", async (ServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderApproveEstimateResult = await handler.AddServiceOrderApproveEstimate(command);
            if (!addServiceOrderApproveEstimateResult.IsSuccess)
                return Results.Ok(addServiceOrderApproveEstimateResult);

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderApproveEstimateResult.Message);

            return Results.Ok(addServiceOrderApproveEstimateResult);
        }).WithTags("ServiceOrder");

        app.MapPut("/add-service-order-reject-estimate", async (ServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderRejectEstimateResult = await handler.AddServiceOrderRejectEstimate(command);
            if (!addServiceOrderRejectEstimateResult.IsSuccess)
                return Results.Ok(addServiceOrderRejectEstimateResult);

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderRejectEstimateResult.Message);

            return Results.Ok(addServiceOrderRejectEstimateResult);
        }).WithTags("ServiceOrder");

        app.MapPut("/add-service-order-purchased-part", async (ServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderPurshasedPartResult = await handler.AddPurchasedPart(command);
            if (!addServiceOrderPurshasedPartResult.IsSuccess)
                return Results.Ok(addServiceOrderPurshasedPartResult);

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderPurshasedPartResult.Message);

            return Results.Ok(addServiceOrderPurshasedPartResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-service-order-repair", async (ServiceOrderHandler handler,NotificationHandler notificationhandler ,GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderRepairResult = await handler.AddServiceOrderRepair(command);
            if (!addServiceOrderRepairResult.IsSuccess)
                return Results.Ok(addServiceOrderRepairResult);
                
            await notificationhandler.CreateNotification("Conserto Concluído", $"Favor avisar o responsável da ordem de serviço: {command.Id}");

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderRepairResult.Message);

            return Results.Ok(addServiceOrderRepairResult);
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-service-order-delivery", async (ServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderDeliveryResult = await handler.AddServiceOrderDeliveryAndReturnPdfAsync(command);
            if (!addServiceOrderDeliveryResult.IsSuccess)
                return Results.Ok(addServiceOrderDeliveryResult);

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderDeliveryResult.Message);

            if (addServiceOrderDeliveryResult.Data != null)
                return Results.File(addServiceOrderDeliveryResult.Data, "application/pdf", "ordem_servico.pdf");
                
            return Results.Ok();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/regenerate-service-order-pdf", async (ServiceOrderHandler handler, GetServiceOrderByIdCommand command) =>
        {
            command.Validate();

            var regenerateServiceOrderResult = await handler.RegenerateAndReturnPdfAsync(command);
            if (!regenerateServiceOrderResult.IsSuccess)
                return Results.Ok(regenerateServiceOrderResult);

            return Results.File(regenerateServiceOrderResult.Data!, "application/pdf", "ordem_servico.pdf");
        }).WithTags("ServiceOrder").RequireAuthorization();
    }
}