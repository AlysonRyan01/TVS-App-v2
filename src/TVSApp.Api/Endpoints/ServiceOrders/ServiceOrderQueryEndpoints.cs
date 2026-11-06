using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.Extensions;
using TVS_App.Api.SignalR;
using TVS_App.Application.Handlers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;

namespace TVS_App.Api.Endpoints.ServiceOrders;

public static class ServiceOrderQueryEndpoints
{
    public static void MapServiceOrderQueryEndpoints(this WebApplication app)
    {
        app.MapGet("/service-orders/pending-estimate",
            async (IServiceOrderQueryHandler handler, int pageNumber = 1, int pageSize = 25) =>
        {
            var result = await handler.GetPendingEstimateServiceOrders(pageNumber, pageSize);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/awaiting-response",
            async (IServiceOrderQueryHandler handler, int pageNumber = 1, int pageSize = 25) =>
        {
            var result = await handler.GetAwaitingResponseServiceOrders(pageNumber, pageSize);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/approved",
            async (IServiceOrderQueryHandler handler, int pageNumber = 1, int pageSize = 25) =>
        {
            var result = await handler.GetApprovedServiceOrders(pageNumber, pageSize);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/rejected",
            async (IServiceOrderQueryHandler handler, int pageNumber = 1, int pageSize = 25) =>
        {
            var result = await handler.GetRejectedServiceOrders(pageNumber, pageSize);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/awaiting-part",
            async (IServiceOrderQueryHandler handler, int pageNumber = 1, int pageSize = 25) =>
        {
            var result = await handler.GetAwaitingPartServiceOrders(pageNumber, pageSize);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/ready-for-pickup",
            async (IServiceOrderQueryHandler handler, int pageNumber = 1, int pageSize = 25) =>
        {
            var result = await handler.GetReadyForPickupServiceOrders(pageNumber, pageSize);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/search",
            async (IServiceOrderQueryHandler handler, [AsParameters] ServiceOrderSearchQuery query) =>
        {
            var result = await handler.Search(query);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/{id:long}",
            async (IServiceOrderQueryHandler handler, long id) =>
        {
            var result = await handler.GetServiceOrderById(id);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/{id:long}/pdf", async (IServiceOrderCommandHandler handler, long id) =>
        {
            var result = await handler.RegeneratePdf(id);
            if (!result.IsSuccess || result.Data == null)
                return result.ToResult();

            return Results.File(result.Data, "application/pdf", $"ordem_servico_{id}.pdf");
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/{id:long}/payments/{paymentId:long}/receipt", async (
                IServiceOrderCommandHandler handler,
                long id,
                long paymentId) =>
        {
            var result = await handler.GetPaymentReceipt(id, paymentId);
            if (!result.IsSuccess || result.Data == null)
                return result.ToResult();

            return Results.File(result.Data, "application/pdf", $"comprovante_pagamento_{paymentId}.pdf");
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/{id:long}/payments", async (IServiceOrderQueryHandler handler, long id) =>
        {
            var result = await handler.GetPayments(id);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();
    }
}
