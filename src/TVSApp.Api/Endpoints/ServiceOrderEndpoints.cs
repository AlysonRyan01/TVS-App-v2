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
        app.MapPost("/create-service-order", async (
                ISender handler, 
                CreateServiceOrderCommand command, 
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var normalizedCommand = command.Normalize();
            normalizedCommand.Validate();

            var createOrderResult = await handler.Send(normalizedCommand);
            if (!createOrderResult.IsSuccess)
                return Results.Ok(createOrderResult);

            await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

            return Results.File(createOrderResult.Data!, "application/pdf", "ordem_servico.pdf");
        })
        .WithTags("ServiceOrder")
        .WithName("CreateServiceOrder")
        .WithSummary("Cria uma nova ordem de serviço e retorna o PDF gerado.")
        .WithDescription("Recebe os dados da ordem de serviço via comando, valida, cria a ordem, "
                         + "gera um PDF e envia notificações via SignalR para os clientes conectados. "
                         + "Retorna o arquivo PDF como resposta.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/pdf")
        .RequireAuthorization();
        
        app.MapPost("/create-sales-service-order", async (
                ISender handler, 
                CreateSalesServiceOrderCommand command, 
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var normalizedCommand = command.Normalize();
            normalizedCommand.Validate();

            var createOrderResult = await handler.Send(normalizedCommand);
            if (!createOrderResult.IsSuccess)
                return Results.Ok(createOrderResult);

            await hubContext.Clients.All.SendAsync("Atualizar", createOrderResult.Message);

            return Results.File(createOrderResult.Data!, "application/pdf", "ordem_servico.pdf");
        })
        .WithTags("ServiceOrder")
        .WithName("CreateSalesServiceOrder")
        .WithSummary("Cria uma ordem de serviço do tipo venda e retorna o PDF gerado (garantia).")
        .WithDescription("Recebe os dados da ordem de serviço de venda via comando, valida os dados, "
                         + "processa a venda, gera o PDF da ordem e envia notificações via SignalR para os clientes conectados.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/pdf")
        .RequireAuthorization();

        app.MapPut("/edit-service-order", async (
                ISender handler, 
                EditServiceOrderCommand command, 
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var editOrderResult = await handler.Send(command);
            if (!editOrderResult.IsSuccess)
                return Results.Ok(editOrderResult);

            await hubContext.Clients.All.SendAsync("Atualizar", editOrderResult.Message);

            return Results.Ok(editOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("EditServiceOrder")
        .WithSummary("Edita uma ordem de serviço existente.")
        .WithDescription("Recebe os dados atualizados da ordem de serviço via comando, processa a edição, "
                         + "e em caso de sucesso, notifica os clientes conectados via SignalR.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapGet("/service-orders/{id:long}",
            async (IServiceOrderQueryHandler handler, long id) =>
        {
            var result = await handler.GetServiceOrderById(id);
            return result.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-service-orders-by-customer-name", async (
                ISender handler, 
                [FromQuery] string name) =>
        {
            var command = new GetServiceOrdersByCustomerNameQuery(name);
            var getOrderResult = await handler.Send(command);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetServiceOrdersByCustomerName")
        .WithSummary("Busca ordens de serviço pelo nome do cliente.")
        .WithDescription("Recebe o nome do cliente via query string e retorna todas as ordens de serviço associadas a esse nome.")
        .Produces<BaseResponse<IEnumerable<ServiceOrderDto>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-serial-number", async (ISender handler, [FromQuery] string serialNumber) =>
        {
            var command = new GetServiceOrdersBySerialNumberQuery(serialNumber);
            command.Validate();
            
            var getOrderResult = await handler.Send(command);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetServiceOrdersBySerialNumber")
        .WithSummary("Busca ordens de serviço pelo número de série do produto.")
        .WithDescription("Recebe o número de série do produto via query string, cria um comando e retorna todas as ordens de serviço associadas ao produto do cliente com esse número de série.")
        .Produces<BaseResponse<IEnumerable<ServiceOrderDto>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-model", async (
                ISender handler, 
                [FromQuery] string model) =>
        {
            var command = new GetServiceOrdersByModelQuery(model);
            command.Validate();
            
            var getOrderResult = await handler.Send(command);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetServiceOrdersByModel")
        .WithSummary("Busca ordens de serviço pelo modelo do produto.")
        .WithDescription("Recebe o modelo do produto via query string, cria o comando e retorna todas as ordens de serviço associadas a esse modelo.")
        .Produces<BaseResponse<IEnumerable<ServiceOrderDto>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
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
        
        app.MapGet("/get-service-orders-by-date", async (
                ISender handler, 
                [FromQuery] DateTime startDate, 
                [FromQuery] DateTime endDate) =>
        {
            var command = new GetServiceOrdersByDateQuery(startDate, endDate);
            command.Validate();
            
            var getOrderResult = await handler.Send(command);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetServiceOrdersByDate")
        .WithSummary("Busca ordens de serviço entre duas datas.")
        .WithDescription("Recebe uma data inicial e uma data final via query string, e retorna todas as ordens de serviço cadastradas dentro desse intervalo.")
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

        app.MapGet("/get-all-service-orders/{pageNumber}/{pageSize}", async (
                ISender handler,
                int pageNumber,
                int pageSize) =>
            {
                var command = new GetAllServiceOrdersQuery(pageNumber, pageSize);
                command.Validate();

                var getOrderResult = await handler.Send(command);

                return Results.Ok(getOrderResult);
            })
        .WithTags("ServiceOrder")
        .WithName("GetAllServiceOrders")
        .WithSummary("Retorna todas as ordens de serviço paginadas.")
        .WithDescription("Lista todas as ordens de serviço do sistema, utilizando paginação com os parâmetros `pageNumber` e `pageSize` informados na rota.")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

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

        app.MapPut("/update-service-order-part-purchase-cost", async (
                ISender handler,
                UpdatePartPurchaseCostCommand command) =>
        {
            var updatePartPurchaseCostResult = await handler.Send(command);
            return Results.Ok(updatePartPurchaseCostResult);
        })
        .WithTags("ServiceOrder")
        .WithName("UpdateServiceOrderPartPurchaseCost")
        .WithSummary("Atualiza o valor pago na compra da peça de uma ordem de serviço.")
        .WithDescription("Recebe o ID da ordem de serviço e o valor pago pela peça, usado para calcular o gasto com peças no dashboard de métricas.")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

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

        app.MapPost("/regenerate-service-order-pdf", async (ISender handler, RegenerateAndReturnPdfCommand command) =>
            {
                var regenerateServiceOrderResult = await handler.Send(command);
                if (!regenerateServiceOrderResult.IsSuccess)
                    return Results.Ok(regenerateServiceOrderResult);

                return Results.File(regenerateServiceOrderResult.Data!, "application/pdf", "ordem_servico.pdf");
            })
        .WithTags("ServiceOrder")
        .WithName("RegenerateServiceOrderPdf")
        .WithSummary("Regenera o PDF de uma ordem de serviço existente.")
        .WithDescription("Recebe o ID da ordem de serviço, regenera o arquivo PDF atualizado e retorna o arquivo para download.")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/pdf")
        .Produces<BaseResponse<byte[]>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
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