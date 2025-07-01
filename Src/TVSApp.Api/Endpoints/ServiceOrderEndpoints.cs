using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TVS_App.Api.SignalR;
using TVS_App.Application.Commands;
using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Interfaces.Handlers;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Api.Endpoints;

public static class ServiceOrderEndpoints
{
    public static void MapServiceOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/create-service-order", async (IServiceOrderHandler handler, CreateServiceOrderCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var createOrderResult = await handler.CreateServiceOrderAndReturnPdfAsync(command);
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
        
        app.MapPost("/create-sales-service-order", async (IServiceOrderHandler handler, CreateSalesServiceOrderCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var createOrderResult = await handler.CreateSalesServiceOrderAsync(command);
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

        app.MapPut("/edit-service-order", async ([FromServices]IServiceOrderHandler handler, [FromBody]EditServiceOrderCommand command, [FromServices]IHubContext<ServiceOrderHub> hubContext) =>
        {
            var editOrderResult = await handler.EditServiceOrderAsync(command);
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

        app.MapGet("/get-service-order-by-id/{id}", async (IServiceOrderHandler handler, long id) =>
        {
            var command = new GetServiceOrderByIdCommand { Id = id };
            command.Validate();

            var getOrderResult = await handler.GetServiceOrderById(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetServiceOrderById")
        .WithSummary("Busca uma ordem de serviço pelo ID.")
        .WithDescription("Recebe o ID da ordem de serviço via rota, executa a validação e busca os dados no banco. "
                         + "Retorna a ordem de serviço encontrada ou uma mensagem de erro.")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapGet("/get-service-orders-by-customer-name", async (IServiceOrderHandler handler, [FromQuery] string name) =>
        {
            var getOrderResult = await handler.GetServiceOrdersByCustomerName(name);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetServiceOrdersByCustomerName")
        .WithSummary("Busca ordens de serviço pelo nome do cliente.")
        .WithDescription("Recebe o nome do cliente via query string e retorna todas as ordens de serviço associadas a esse nome.")
        .Produces<BaseResponse<IEnumerable<ServiceOrderDto>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-serial-number", async (IServiceOrderHandler handler, [FromQuery] string serialNumber) =>
        {
            var getOrderResult = await handler
                .GetServiceOrdersBySerialNumberAsync(
                    new GetServiceOrdersBySerialNumberCommand { SerialNumber = serialNumber });
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetServiceOrdersBySerialNumber")
        .WithSummary("Busca ordens de serviço pelo número de série do produto.")
        .WithDescription("Recebe o número de série do produto via query string, cria um comando e retorna todas as ordens de serviço associadas ao produto do cliente com esse número de série.")
        .Produces<BaseResponse<IEnumerable<ServiceOrderDto>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-model", async (IServiceOrderHandler handler, [FromQuery] string model) =>
        {
            var getOrderResult = await handler
                .GetServiceOrdersByModelAsync(
                    new GetServiceOrdersByModelCommand() { Model = model });
                
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetServiceOrdersByModel")
        .WithSummary("Busca ordens de serviço pelo modelo do produto.")
        .WithDescription("Recebe o modelo do produto via query string, cria o comando e retorna todas as ordens de serviço associadas a esse modelo.")
        .Produces<BaseResponse<IEnumerable<ServiceOrderDto>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
        
        app.MapGet("/get-service-orders-by-enterprise", async (IServiceOrderHandler handler, [FromQuery] EEnterprise enterprise) =>
        {
            var getOrderResult = await handler
                .GetServiceOrdersByEnterpriseAsync(enterprise);
                
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

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
        
        app.MapGet("/get-service-orders-by-date", async (IServiceOrderHandler handler, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate) =>
        {
            var getOrderResult = await handler
                .GetServiceOrdersByDateAsync(startDate, endDate);
                
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetServiceOrdersByDate")
        .WithSummary("Busca ordens de serviço entre duas datas.")
        .WithDescription("Recebe uma data inicial e uma data final via query string, e retorna todas as ordens de serviço cadastradas dentro desse intervalo.")
        .Produces<BaseResponse<IEnumerable<ServiceOrderDto>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapGet("/get-service-order-for-customer/{id}/{code}", async (IServiceOrderHandler handler, long id, string code) =>
        {
            var command = new GetServiceOrdersForCustomerCommand { ServiceOrderId = id, SecurityCode = code };
            command.Validate();

            var getOrderResult = await handler.GetServiceOrderForCustomer(command);

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

        app.MapGet("/get-all-service-orders/{pageNumber}/{pageSize}", async (IServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetAllServiceOrdersAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetAllServiceOrders")
        .WithSummary("Retorna todas as ordens de serviço paginadas.")
        .WithDescription("Lista todas as ordens de serviço do sistema, utilizando paginação com os parâmetros `pageNumber` e `pageSize` informados na rota.")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapGet("/get-pending-estimates-service-orders/{pageNumber}/{pageSize}", async (IServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetPendingEstimatesAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetPendingEstimatesServiceOrders")
        .WithSummary("Lista ordens de serviço com orçamentos pendentes.")
        .WithDescription("Retorna uma lista paginada de ordens de serviço cujo orçamento está pendente.")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapGet("/get-waiting-response-service-orders/{pageNumber}/{pageSize}", async (IServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetWaitingResponseAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetWaitingResponseServiceOrders")
        .WithSummary("Lista ordens de serviço aguardando resposta do cliente (aprovação ou reprovação).")
        .WithDescription("Retorna uma lista paginada de ordens de serviço cujo status está aguardando a resposta do cliente para aprovação ou reprovação do orçamento ou serviço.")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapGet("/get-pending-purchase-service-orders/{pageNumber}/{pageSize}", async (IServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetPendingPurchasePartAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetPendingPurchaseServiceOrders")
        .WithSummary("Lista ordens de serviço com compra de peça pendente.")
        .WithDescription("Retorna uma lista paginada de ordens de serviço que estão aguardando a compra de peças para continuidade do conserto.")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapGet("/get-waiting-parts-service-orders/{pageNumber}/{pageSize}", async (IServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetWaitingPartsAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetWaitingPartsServiceOrders")
        .WithSummary("Lista ordens de serviço aguardando chegada das peças.")
        .WithDescription("Retorna uma lista paginada de ordens de serviço cujo status indica que estão aguardando a chegada das peças necessárias para o conserto.")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapGet("/get-waiting-pickup-service-orders/{pageNumber}/{pageSize}", async (IServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetWaitingPickupAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetWaitingPickupServiceOrders")
        .WithSummary("Lista ordens de serviço aguardando retirada pelo cliente.")
        .WithDescription("Retorna uma lista paginada de ordens de serviço cujo status indica que estão prontas e aguardando o cliente retirar o equipamento.")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapGet("/get-delivered-service-orders/{pageNumber}/{pageSize}", async (IServiceOrderHandler handler, int pageNumber, int pageSize) =>
        {
            var command = new PaginationCommand { PageNumber = pageNumber, PageSize = pageSize };
            command.Validate();

            var getOrderResult = await handler.GetDeliveredAsync(command);
            if (!getOrderResult.IsSuccess)
                return Results.Ok(getOrderResult);

            return Results.Ok(getOrderResult);
        })
        .WithTags("ServiceOrder")
        .WithName("GetDeliveredServiceOrders")
        .WithSummary("Lista ordens de serviço entregues aos clientes.")
        .WithDescription("Retorna uma lista paginada de ordens de serviço cujo status indica que foram entregues aos clientes.")
        .Produces<BaseResponse<PaginatedResult<ServiceOrderDto?>>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapPut("/add-product-location", async (IServiceOrderHandler handler, AddProductLocationCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addLocationResult = await handler.AddProductLocation(command);
            if (!addLocationResult.IsSuccess)
                return Results.Ok(addLocationResult);

            await hubContext.Clients.All.SendAsync("Atualizar", addLocationResult.Message);

            return Results.Ok(addLocationResult);
        })
        .WithTags("ServiceOrder")
        .WithName("AddProductLocation")
        .WithSummary("Adiciona a localização de um produto na prateleira.")
        .WithDescription("Recebe as informações necessárias para registrar ou atualizar o local físico do produto dentro da prateleira da empresa.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapPut("/add-service-order-estimate", async (IServiceOrderHandler handler, INotificationHandler notificationhandler,AddServiceOrderEstimateCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderEstimateResult = await handler.AddServiceOrderEstimate(command);
            if (!addServiceOrderEstimateResult.IsSuccess)
                return Results.Ok(addServiceOrderEstimateResult);

            await notificationhandler.CreateNotification("Orçamento Concluído", $"Favor enviar mensagem de orçamento para o responsável da ordem de serviço: {command.ServiceOrderId}");

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderEstimateResult.Message);

            return Results.Ok(addServiceOrderEstimateResult);
        })
        .WithTags("ServiceOrder")
        .WithName("AddServiceOrderEstimate")
        .WithSummary("Adiciona um orçamento a uma ordem de serviço.")
        .WithDescription("Recebe os dados do orçamento para uma ordem de serviço específica, salva a informação e notifica os clientes conectados via SignalR.")
        .Produces<BaseResponse<string>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapPut("/add-service-order-approve-estimate", async (IServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderApproveEstimateResult = await handler.AddServiceOrderApproveEstimate(command);
            if (!addServiceOrderApproveEstimateResult.IsSuccess)
                return Results.Ok(addServiceOrderApproveEstimateResult);

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderApproveEstimateResult.Message);

            return Results.Ok(addServiceOrderApproveEstimateResult);
        })
        .WithTags("ServiceOrder")
        .WithName("AddServiceOrderApproveEstimate")
        .WithSummary("Registra a aprovação do orçamento para uma ordem de serviço.")
        .WithDescription("Recebe o ID da ordem de serviço, valida e registra a aprovação do orçamento pelo cliente, notificando os clientes conectados via SignalR.")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapPut("/add-service-order-reject-estimate", async (IServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderRejectEstimateResult = await handler.AddServiceOrderRejectEstimate(command);
            if (!addServiceOrderRejectEstimateResult.IsSuccess)
                return Results.Ok(addServiceOrderRejectEstimateResult);

            await hubContext.Clients.All.SendAsync("Atualizar", addServiceOrderRejectEstimateResult.Message);

            return Results.Ok(addServiceOrderRejectEstimateResult);
        })
        .WithTags("ServiceOrder")
        .WithName("AddServiceOrderRejectEstimate")
        .WithSummary("Registra a rejeição do orçamento para uma ordem de serviço.")
        .WithDescription("Recebe o ID da ordem de serviço, valida e registra a rejeição do orçamento pelo cliente, notificando os clientes conectados via SignalR.")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status200OK, "application/json")
        .Produces<BaseResponse<ServiceOrderDto?>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();

        app.MapPut("/add-service-order-purchased-part", async (IServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderPurshasedPartResult = await handler.AddPurchasedPart(command);
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

        app.MapPut("/add-service-order-repair", async (IServiceOrderHandler handler,INotificationHandler notificationhandler ,GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderRepairResult = await handler.AddServiceOrderRepair(command);
            if (!addServiceOrderRepairResult.IsSuccess)
                return Results.Ok(addServiceOrderRepairResult);
                
            await notificationhandler.CreateNotification("Conserto Concluído", $"Favor avisar o responsável da ordem de serviço: {command.Id}");

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

        app.MapPut("/add-service-order-delivery", async (IServiceOrderHandler handler, GetServiceOrderByIdCommand command, IHubContext<ServiceOrderHub> hubContext) =>
        {
            command.Validate();

            var addServiceOrderDeliveryResult = await handler.AddServiceOrderDeliveryAndReturnPdfAsync(command);
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

        app.MapPut("/regenerate-service-order-pdf", async (IServiceOrderHandler handler, GetServiceOrderByIdCommand command) =>
        {
            command.Validate();

            var regenerateServiceOrderResult = await handler.RegenerateAndReturnPdfAsync(command);
            if (!regenerateServiceOrderResult.IsSuccess)
                return Results.Ok(regenerateServiceOrderResult);

            return Results.File(regenerateServiceOrderResult.Data!, "application/pdf", "ordem_servico.pdf");
        })
        .WithTags("ServiceOrder")
        .WithName("RegenerateServiceOrderPdf")
        .WithSummary("Regenera o PDF de uma ordem de serviço existente.")
        .WithDescription("Recebe o ID da ordem de serviço, regenera o arquivo PDF atualizado e retorna o arquivo para download.")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/pdf")
        .Produces<BaseResponse<string>>(StatusCodes.Status400BadRequest, "application/json")
        .RequireAuthorization();
    }
}