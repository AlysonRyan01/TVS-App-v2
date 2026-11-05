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

        app.MapGet("/get-service-order-by-id/{id}", 
            async (IServiceOrderQueryHandler handler, long id) =>
            {
                var getOrderResult = await handler.GetServiceOrderById(id);
                return getOrderResult.ToResult();
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

        app.MapGet("/get-pending-estimates-service-orders/{pageNumber}/{pageSize}", 
            async (IServiceOrderQueryHandler handler, int pageNumber, int pageSize) =>
        {
            var getOrderResult = await handler.GetPendingEstimateServiceOrders(pageNumber, pageSize);
            return getOrderResult.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-waiting-response-service-orders/{pageNumber}/{pageSize}", 
            async (IServiceOrderQueryHandler handler, int pageNumber, int pageSize) =>
        {
            var getOrderResult = await handler.GetWaitingResponseServiceOrders(pageNumber, pageSize);
            return getOrderResult.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-pending-purchase-service-orders/{pageNumber}/{pageSize}", 
            async (IServiceOrderQueryHandler handler, int pageNumber, int pageSize) =>
        {
            var getOrderResult = await handler.GetApprovedServiceOrders(pageNumber, pageSize);
            return getOrderResult.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();
        
        app.MapGet("/get-rejected-estimate-service-orders/{pageNumber}/{pageSize}", 
            async (IServiceOrderQueryHandler handler, int pageNumber, int pageSize) =>
        {
            var getOrderResult = await handler.GetRefusedServiceOrders(pageNumber, pageSize);
            return getOrderResult.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-waiting-parts-service-orders/{pageNumber}/{pageSize}", 
            async (IServiceOrderQueryHandler handler, int pageNumber, int pageSize) =>
        {
            var getOrderResult = await handler.GetWaitingPartServiceOrders(pageNumber, pageSize);
            return getOrderResult.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapGet("/get-waiting-pickup-service-orders/{pageNumber}/{pageSize}", 
            async (IServiceOrderQueryHandler handler, int pageNumber, int pageSize) =>
        {
            var getOrderResult = await handler.GetReadyServiceOrders(pageNumber, pageSize);
            return getOrderResult.ToResult();
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

        app.MapPut("/add-product-location", async (
                ISender handler, 
                AddProductLocationCommand command, 
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var normalizedCommand = command.Normalize();
            normalizedCommand.Validate();

            var addLocationResult = await handler.Send(normalizedCommand);
            await hubContext.Clients.All.SendAsync("Atualizar", addLocationResult.Message);
            return Results.Ok(addLocationResult);
        })
        .WithTags("ServiceOrder")
        .WithName("AddProductLocation")
        .WithSummary("Adiciona a localização de um produto na prateleira.")
        .WithDescription("Recebe as informações necessárias para registrar ou atualizar o local físico do produto")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapPut("/add-service-order-estimate", async (
                ISender handler, 
                AddServiceOrderEstimateCommand command, 
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var normalizedCommand = command.Normalize();
            normalizedCommand.Validate();

            var addServiceOrderEstimateResult = await handler.Send(normalizedCommand);
            if (!addServiceOrderEstimateResult.IsSuccess)
                return addServiceOrderEstimateResult.ToResult();

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderEstimateResult.Message);

            return addServiceOrderEstimateResult.ToResult();
        }).WithTags("ServiceOrder").RequireAuthorization();

        app.MapPut("/add-service-order-approve-estimate", async (
                ISender handler, 
                AddServiceOrderApproveEstimateCommand command, 
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var addServiceOrderApproveEstimateResult = await handler.Send(command);
            if (!addServiceOrderApproveEstimateResult.IsSuccess)
                return Results.Ok(addServiceOrderApproveEstimateResult);
            
            var notificationCommand = new CreateNotificationCommand(
                $"Orçamento Aprovado, OS: {command.Id}",
                $"Favor comprar peça para OS: {command.Id}");
            
            await handler.Send(notificationCommand);

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderApproveEstimateResult.Message);

            return Results.Ok(addServiceOrderApproveEstimateResult);
        })
        .WithTags("ServiceOrder")
        .WithName("AddServiceOrderApproveEstimate")
        .WithSummary("Registra a aprovação do orçamento para uma ordem de serviço.")
        .WithDescription("Recebe o ID da ordem de serviço, valida e registra a aprovação do orçamento pelo cliente, notificando os clientes conectados via SignalR.")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status400BadRequest, "application/json");

        app.MapPut("/add-service-order-reject-estimate", async (
                ISender handler, 
                AddServiceOrderRejectEstimateCommand command, 
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var addServiceOrderRejectEstimateResult = await handler.Send(command);
            if (!addServiceOrderRejectEstimateResult.IsSuccess)
                return Results.Ok(addServiceOrderRejectEstimateResult);
            
            var notificationCommand = new CreateNotificationCommand(
                $"Orçamento Reprovado, OS: {command.Id}",
                $"Favor montar a OS {command.Id} para entrega");
            
            await handler.Send(notificationCommand);

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderRejectEstimateResult.Message);

            return Results.Ok(addServiceOrderRejectEstimateResult);
        })
        .WithTags("ServiceOrder")
        .WithName("AddServiceOrderRejectEstimate")
        .WithSummary("Registra a rejeição do orçamento para uma ordem de serviço.")
        .WithDescription("Recebe o ID da ordem de serviço, valida e registra a rejeição do orçamento pelo cliente, notificando os clientes conectados via SignalR.")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status400BadRequest, "application/json");
        
        app.MapPut("/mark-estimate-rejected-as-ready-to-pick-up", async (
                ISender handler, 
                MarkEstimateRejectedAsReadyToPickUpCommand command, 
                IHubContext<ServiceOrderHub> hubContext) =>
            {
                var markServiceOrderResult = await handler.Send(command);
                if (!markServiceOrderResult.IsSuccess)
                    return Results.Ok(markServiceOrderResult);

                await hubContext.Clients.All.SendAsync("Atualizar", markServiceOrderResult.Message);

                return Results.Ok(markServiceOrderResult);
            })
            .WithTags("ServiceOrder")
            .WithName("MarkEstimateRejectedAsReadyToPickUp")
            .WithSummary("Marca a ordem de serviço como pronta para entrega.")
            .WithDescription("Recebe o ID da ordem de serviço, valida e marca a ordem de serviço como pronta para entrega.")
            .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status200OK, "application/json")
            .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status400BadRequest, "application/json")
            .RequireAuthorization();

        app.MapPut("/add-service-order-purchased-part", async (
                ISender handler, 
                AddPurchasedPartCommand command, 
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var addServiceOrderPurshasedPartResult = await handler.Send(command);
            if (!addServiceOrderPurshasedPartResult.IsSuccess)
                return Results.Ok(addServiceOrderPurshasedPartResult);

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderPurshasedPartResult.Message);

            return Results.Ok(addServiceOrderPurshasedPartResult);
        })
        .WithTags("ServiceOrder")
        .WithName("AddServiceOrderPurchasedPart")
        .WithSummary("Registra a compra da peça para uma ordem de serviço.")
        .WithDescription("Recebe o ID da ordem de serviço, valida e registra que a peça necessária foi comprada, notificando os clientes conectados via SignalR.")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

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

        app.MapPut("/add-service-order-repair", async (
                ISender handler, 
                AddServiceOrderRepairCommand command, 
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var addServiceOrderRepairResult = await handler.Send(command);
            if (!addServiceOrderRepairResult.IsSuccess)
                return Results.Ok(addServiceOrderRepairResult);

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderRepairResult.Message);

            return Results.Ok(addServiceOrderRepairResult);
        })
        .WithTags("ServiceOrder")
        .WithName("AddServiceOrderRepair")
        .WithSummary("Registra o conserto concluído em uma ordem de serviço.")
        .WithDescription("Recebe o ID da ordem de serviço, valida e registra que o conserto foi concluído, cria uma notificação e notifica os clientes conectados via SignalR.")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapPut("/add-service-order-delivery", async (
                ISender handler, 
                AddServiceOrderDeliveryAndReturnPdfCommand command, 
                IHubContext<ServiceOrderHub> hubContext) =>
        {
            var addServiceOrderDeliveryResult = await handler.Send(command);
            if (!addServiceOrderDeliveryResult.IsSuccess)
                return Results.Ok(addServiceOrderDeliveryResult);

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderDeliveryResult.Message);

            if (addServiceOrderDeliveryResult.Data != null)
                return Results.File(addServiceOrderDeliveryResult.Data, "application/pdf", "ordem_servico.pdf");
                
            return Results.Ok();
        })
        .WithTags("ServiceOrder")
        .WithName("AddServiceOrderDelivery")
        .WithSummary("Registra a entrega da ordem de serviço e retorna o PDF correspondente.")
        .WithDescription("Recebe o ID da ordem de serviço, registra sua entrega, notifica os clientes conectados via SignalR e retorna o arquivo PDF da ordem de serviço se disponível.")
        .Produces<BaseResponse<byte[]>>(StatusCodes.Status200OK, "application/pdf")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

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