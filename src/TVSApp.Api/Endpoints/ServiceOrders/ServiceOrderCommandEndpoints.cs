using TVS_App.Api.Observability;
using TVS_App.Application.Observability;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.Extensions;
using TVS_App.Api.SignalR;
using TVS_App.Application.Handlers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

namespace TVS_App.Api.Endpoints.ServiceOrders;

public static class ServiceOrderCommandEndpoints
{
    public static void MapServiceOrderCommandEndpoints(this WebApplication app)
    {
        app.MapPost("/service-orders", async (
                IServiceOrderCommandHandler handler,
                CreateServiceOrderCommand command,
                HttpContext httpContext,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.CreateServiceOrder(command);
            if (!result.IsSuccess || result.Data == null)
                return result.ToResult();

            await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            httpContext.Response.Headers["X-Service-Order-Id"] = result.Data.Id.ToString();
            return Results.File(result.Data.Pdf, "application/pdf", $"ordem_servico_{result.Data.Id}.pdf");
        }).WithBusinessEvent(BusinessEvents.ServiceOrderCreated).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPost("/service-orders/sales", async (
                IServiceOrderCommandHandler handler,
                CreateSalesServiceOrderCommand command,
                HttpContext httpContext,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.CreateSalesServiceOrder(command);
            if (!result.IsSuccess || result.Data == null)
                return result.ToResult();

            await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            httpContext.Response.Headers["X-Service-Order-Id"] = result.Data.Id.ToString();
            return Results.File(result.Data.Pdf, "application/pdf", $"venda_{result.Data.Id}.pdf");
        }).WithBusinessEvent(BusinessEvents.SaleCreated).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}", async (
                IServiceOrderCommandHandler handler,
                long id,
                EditServiceOrderCommand command,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.UpdateServiceOrder(id, command);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.ServiceOrderUpdated).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/estimate", async (
                IServiceOrderCommandHandler handler,
                long id,
                AddServiceOrderEstimateCommand command,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.AddEstimate(command with { ServiceOrderId = id });
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.EstimateAdded).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/estimate/approve", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.ApproveEstimate(id);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.EstimateApproved).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/estimate/reject", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.RejectEstimate(id);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.EstimateRejected).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/ready-for-pickup", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.MarkRejectedEstimateAsReadyForPickup(id);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.ServiceOrderReadyForPickup).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/part/purchase", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.RegisterPartPurchase(id);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.PartPurchased).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/part/purchase-cost", async (
                IServiceOrderCommandHandler handler,
                long id,
                UpdatePartPurchaseCostRequest body) =>
        {
            var result = await handler.UpdatePartPurchaseCost(id, body.PartPurchaseCost);
            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.PartPurchaseCostUpdated).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/repair", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.RegisterRepair(id);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.ServiceOrderRepaired).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/delivery", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.RegisterDelivery(id);
            if (!result.IsSuccess)
                return result.ToResult();

            await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            if (result.Data != null)
                return Results.File(result.Data, "application/pdf", $"ordem_servico_{id}.pdf");

            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.ServiceOrderDelivered).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/product/location", async (
                IServiceOrderCommandHandler handler,
                long id,
                AddProductLocationCommand command,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.AddProductLocation(command with { ServiceOrderId = id });
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.ServiceOrderLocationUpdated).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPost("/service-orders/{id:long}/payments", async (
                IServiceOrderCommandHandler handler,
                long id,
                AddPaymentCommand command) =>
        {
            var result = await handler.AddPayment(id, command);
            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.PaymentAdded).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/payments/{paymentId:long}", async (
                IServiceOrderCommandHandler handler,
                long id,
                long paymentId,
                UpdatePaymentCommand command) =>
        {
            var result = await handler.UpdatePayment(id, paymentId, command);
            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.PaymentUpdated).WithTags("ServiceOrder").RequireAuthorization();

        app.MapDelete("/service-orders/{id:long}/payments/{paymentId:long}", async (
                IServiceOrderCommandHandler handler,
                long id,
                long paymentId) =>
        {
            var result = await handler.RemovePayment(id, paymentId);
            return result.ToResult();
        }).WithBusinessEvent(BusinessEvents.PaymentRemoved).WithTags("ServiceOrder").RequireAuthorization();
    }
}

public record UpdatePartPurchaseCostRequest(decimal PartPurchaseCost);
