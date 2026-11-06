using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.Extensions;
using TVS_App.Api.SignalR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Handlers;
using TVS_App.Application.Requests.NotificationRequests.NotificationCommands;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Api.Endpoints;

public static class ServiceOrderEndpoints
{
    public static void MapServiceOrderEndpoints(this WebApplication app)
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

            // O número da OS vai no cabeçalho; o corpo é o termo de entrada em PDF.
            httpContext.Response.Headers["X-Service-Order-Id"] = result.Data.Id.ToString();
            return Results.File(result.Data.Pdf, "application/pdf", $"ordem_servico_{result.Data.Id}.pdf");
        }).WithTags("ServiceOrder").RequireAuthorization();
        
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

            // Venda = OS com defeito "VENDA". O número vai no cabeçalho; o corpo é o comprovante de venda.
            httpContext.Response.Headers["X-Service-Order-Id"] = result.Data.Id.ToString();
            return Results.File(result.Data.Pdf, "application/pdf", $"venda_{result.Data.Id}.pdf");
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/search",
            async (IServiceOrderQueryHandler handler, [AsParameters] ServiceOrderSearchQuery query) =>
        {
            var result = await handler.Search(query);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        // Alterar OS (painel da Buscar OS): só os dados da OS, sem cliente nem situação.
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
        }).WithTags("ServiceOrder").RequireAuthorization();

        // Reimprimir: o documento certo para a situação da OS (entrada, entrega ou venda).
        app.MapGet("/service-orders/{id:long}/pdf", async (IServiceOrderCommandHandler handler, long id) =>
        {
            var result = await handler.RegeneratePdf(id);
            if (!result.IsSuccess || result.Data == null)
                return result.ToResult();

            return Results.File(result.Data, "application/pdf", $"ordem_servico_{id}.pdf");
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/service-orders/{id:long}",
            async (IServiceOrderQueryHandler handler, long id) =>
        {
            var result = await handler.GetServiceOrderById(id);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-service-orders-by-enterprise", async (ISender handler, [FromQuery] EEnterprise enterprise) =>
        {
            var command = new GetServiceOrdersByEnterpriseQuery(enterprise);
            var getOrderResult = await handler.Send(command);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetServiceOrdersByEnterprise")
        .WithSummary("Busca ordens de serviço por tipo de empresa.")
        .WithDescription("Recebe o tipo da empresa via query string (valores do enum `EEnterprise`) "
                         + "e retorna todas as ordens de serviço associadas à empresa informada.")
        .Produces<BaseResponse<IEnumerable<ServiceOrderDto>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
        app.MapGet("/get-service-order-for-customer/{id}/{code}", async (
                ISender handler,
                long id,
                string code) =>
            {
                var command = new GetServiceOrdersForCustomerQuery(id, code);
                command.Validate();

                var getOrderResult = await handler.Send(command);

                return Results.Ok(getOrderResult);
            })
        .WithTags("ServiceOrder")
        .WithName("GetServiceOrderForCustomer")
        .WithSummary("Consulta pública de ordem de serviço pelo cliente.")
        .WithDescription("Permite que o cliente acompanhe sua ordem de serviço informando o número da OS e o código de segurança gerado no momento do cadastro. "
                         + "Este endpoint é público e não exige autenticação."
                         + "Consulta via QR CODE")
        .Produces<BaseResponse<ServiceOrderDto>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json");

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

        app.MapGet("/get-delivered-service-orders/{pageNumber}/{pageSize}", async (
                ISender handler,
                int pageNumber, 
                int pageSize) =>
                {
                    var command = new GetDeliveredQuery(pageNumber, pageSize);
                    command.Validate();

                    var getOrderResult = await handler.Send(command);

                    return Results.Ok(getOrderResult);
                })
        .WithTags("ServiceOrder")
        .WithName("GetDeliveredServiceOrders")
        .WithSummary("Lista ordens de serviço entregues aos clientes.")
        .WithDescription("Retorna uma lista paginada de ordens de serviço cujo status indica que foram entregues aos clientes.")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/product/location", async (
                IServiceOrderCommandHandler handler,
                long id,
                AddProductLocationCommand command,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            // O número da OS vem da rota; o corpo traz só a localização.
            var result = await handler.AddProductLocation(command with { ServiceOrderId = id });
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-service-order-estimate", async (
                IServiceOrderCommandHandler handler,
                AddServiceOrderEstimateCommand command,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.AddEstimate(command);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/estimate/approve", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.ApproveEstimate(id);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/estimate/reject", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.RejectEstimate(id);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/ready-for-pickup", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.MarkRejectedEstimateAsReadyForPickup(id);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/part/purchase", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.RegisterPartPurchase(id);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        // Valor pago na peça (tabela de entregues da tela Métricas).
        app.MapPut("/service-orders/{id:long}/part/purchase-cost", async (
                IServiceOrderCommandHandler handler,
                long id,
                UpdatePartPurchaseCostRequest body) =>
        {
            var result = await handler.UpdatePartPurchaseCost(id, body.PartPurchaseCost);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/repair", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.RegisterRepair(id);
            if (result.IsSuccess)
                await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/service-orders/{id:long}/delivery", async (
                IServiceOrderCommandHandler handler,
                long id,
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var result = await handler.RegisterDelivery(id);
            if (!result.IsSuccess)
                return result.ToResult();

            await hubContext.Clients.All.SendAsync("Atualizar", result.Message);

            // OS consertada: devolve o termo de entrega em PDF (como a rota antiga).
            if (result.Data != null)
                return Results.File(result.Data, "application/pdf", $"ordem_servico_{id}.pdf");

            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPost("/add-payment", async (ISender handler, AddPaymentCommand command) =>
        {
            var addPaymentResult = await handler.Send(command);
            
            return Results.Ok(addPaymentResult);
        })
        .WithTags("ServiceOrder")
        .WithName("AddPayment")
        .WithSummary("Adiciona um pagamento na ordem de serviço.")
        .WithDescription("Recebe o ID da ordem de serviço, e cria o pagamento.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
        app.MapDelete("/remove-payment", async (ISender handler, [AsParameters] RemovePaymentCommand command) =>
        {
            var removePaymentResult = await handler.Send(command);

            return Results.Ok(removePaymentResult);
        })
        .WithTags("ServiceOrder")
        .WithName("RemovePayment")
        .WithSummary("Remove um pagamento de uma ordem de serviço.")
        .WithDescription("Recebe o ID da ordem de serviço e remove o pagamento.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
        app.MapPut("/update-payment", async (ISender handler, UpdatePaymentCommand command) =>
        {
            var updatePaymentResult = await handler.Send(command);
    
            return Results.Ok(updatePaymentResult);
        })
        .WithTags("ServiceOrder")
        .WithName("UpdatePayment")
        .WithSummary("Atualiza um pagamento de uma ordem de serviço.")
        .WithDescription("Recebe o ID da ordem de serviço e atualiza o pagamento.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
        app.MapGet("/get-payments", async (ISender handler, [AsParameters] GetPaymentsQuery command) =>
        {
            var getPaymentsResult = await handler.Send(command);

            return Results.Ok(getPaymentsResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetPayments")
        .WithSummary("Retorna os pagamentos de uma ordem de serviço.")
        .WithDescription("Recebe o ID da ordem de serviço e retorna os pagamentos.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
    }
}

public record UpdatePartPurchaseCostRequest(decimal PartPurchaseCost);
