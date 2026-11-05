using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Application.Handlers;

public class ServiceOrderCommandHandler : IServiceOrderCommandHandler
{
    private readonly IServiceOrderQueryRepository _queryRepository;
    private readonly IServiceOrderCommandRepository _commandRepository;
    private readonly IWhatsAppQueue _whatsAppQueue;
    private readonly INotificationCommandRepository _notificationRepository;
    private readonly IGenerateServiceOrderPdf _pdfGenerator;

    public ServiceOrderCommandHandler(
        IServiceOrderQueryRepository queryRepository,
        IServiceOrderCommandRepository commandRepository,
        IWhatsAppQueue whatsAppQueue,
        INotificationCommandRepository notificationRepository,
        IGenerateServiceOrderPdf pdfGenerator)
    {
        _queryRepository = queryRepository;
        _commandRepository = commandRepository;
        _whatsAppQueue = whatsAppQueue;
        _notificationRepository = notificationRepository;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<BaseResponse<ServiceOrderDto?>> AddEstimate(AddServiceOrderEstimateCommand command)
    {
        var normalized = command.Normalize();
        normalized.Validate();

        var result = await _queryRepository.GetByIdForUpdateAsync(normalized.ServiceOrderId);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.AddEstimate(
            normalized.Solution,
            normalized.Warranty,
            normalized.TotalCost,
            normalized.RepairResult,
            normalized.EstimateMessage);

        serviceOrder.EditPartPurchaseCost(new PartPurchaseCost(normalized.PartPurchaseCost));

        await _commandRepository.UpdateAsync(serviceOrder);

        if (serviceOrder.Enterprise != EEnterprise.PhilcoBritania)
        {
            _whatsAppQueue.Enqueue(new SendEstimateMessageRequest(
                serviceOrder.Id.ToString(),
                serviceOrder.SecurityCode,
                serviceOrder.RepairResult!.Value,
                serviceOrder.Customer.Name.CustomerName,
                normalized.EstimateMessage.ToUpper(),
                serviceOrder.Customer.Phone.CustomerPhone));
        }

        var dto = serviceOrder.MapToServiceOrderDto();

        return new BaseResponse<ServiceOrderDto?>(dto, 200, "Orçamento adicionado com sucesso!");
    }

    // Efeitos de WhatsApp iguais aos handlers antigos (old-handlers/AddServiceOrderApproveEstimateHandler.cs
    // e AddServiceOrderRejectEstimateHandler.cs): a aprovação não avisa na PhilcoBritania; a reprovação avisa sempre.
    public Task<BaseResponse<ServiceOrderDto?>> ApproveEstimate(long id)
        => RespondToEstimate(
            id,
            serviceOrder => serviceOrder.ApproveEstimate(),
            notificationTitle: $"Orçamento Aprovado, OS: {id}",
            notificationMessage: $"Favor comprar peça para OS: {id}",
            enqueueWhatsApp: serviceOrder =>
            {
                if (serviceOrder.Enterprise == EEnterprise.PhilcoBritania)
                    return;

                _whatsAppQueue.Enqueue(new SendThankYouForEstimateApprovalRequest(
                    serviceOrder.Id.ToString(),
                    serviceOrder.Customer.Name.CustomerName,
                    serviceOrder.Customer.Phone.CustomerPhone,
                    serviceOrder.TotalAmount));

                _whatsAppQueue.Enqueue(new NotifyOwnerEstimateDecisionRequest(
                    serviceOrder.Id.ToString(),
                    ERepairStatus.Approved,
                    serviceOrder.Customer.Name.CustomerName));
            },
            successMessage: "Aprovação adicionada com sucesso!");

    public Task<BaseResponse<ServiceOrderDto?>> RejectEstimate(long id)
        => RespondToEstimate(
            id,
            serviceOrder => serviceOrder.RejectEstimate(),
            notificationTitle: $"Orçamento Reprovado, OS: {id}",
            notificationMessage: $"Favor montar a OS {id} para entrega",
            enqueueWhatsApp: serviceOrder =>
            {
                _whatsAppQueue.Enqueue(new SendEstimateDisapprovalAcknowledgmentRequest(
                    serviceOrder.Id.ToString(),
                    serviceOrder.Customer.Name.CustomerName,
                    serviceOrder.Customer.Phone.CustomerPhone));

                _whatsAppQueue.Enqueue(new NotifyOwnerEstimateDecisionRequest(
                    serviceOrder.Id.ToString(),
                    ERepairStatus.Disapproved,
                    serviceOrder.Customer.Name.CustomerName));
            },
            successMessage: "Orçamento reprovado com sucesso!");

    // Igual a old-handlers/AddPurchasedPartHandler.cs: sem validações extras, sem notificação e sem WhatsApp.
    public async Task<BaseResponse<ServiceOrderDto?>> RegisterPartPurchase(long id)
    {
        var result = await _queryRepository.GetByIdForUpdateAsync(id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.AddPurchasedPart();

        await _commandRepository.UpdateAsync(serviceOrder);

        return new BaseResponse<ServiceOrderDto?>(
            serviceOrder.MapToServiceOrderDto(), 200, "Compra de peça adicionada com sucesso!");
    }

    // Igual a old-handlers/MarkEstimateRejectedAsReadyToPickUpHandler.cs (mesma ordem de checagens e mensagens).
    // Se o orçamento não estiver reprovado, a EntityException do domínio sobe e o ExceptionMiddleware responde 400.
    public async Task<BaseResponse<ServiceOrderDto?>> MarkRejectedEstimateAsReadyForPickup(long id)
    {
        if (id == 0)
            return new BaseResponse<ServiceOrderDto?>(null, 400, "O número da ordem de serviço não pode ser 0");

        var serviceOrderResult = await _queryRepository.GetByIdForUpdateAsync(id);
        if (!serviceOrderResult.IsSuccess || serviceOrderResult.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, serviceOrderResult.Message);

        var serviceOrder = serviceOrderResult.Data;

        serviceOrder.MarkEstimateRejectedAsReadyToPickUp();

        var updateResult = await _commandRepository.UpdateAsync(serviceOrder);
        if (!updateResult.IsSuccess)
            return new BaseResponse<ServiceOrderDto?>(null, updateResult.Code, updateResult.Message);

        _whatsAppQueue.Enqueue(new SendRejectionReadyForPickupMessageRequest(
            serviceOrder.Customer.Name.CustomerName,
            serviceOrder.Customer.Phone.CustomerPhone));

        return new BaseResponse<ServiceOrderDto?>(
            serviceOrder.MapToServiceOrderDto(), 200, "Ordem de serviço marcada como pronta para entrega");
    }

    // Igual a old-handlers/AddServiceOrderRepairHandler.cs: não checa o resultado do salvamento e avisa todas as empresas.
    // Se o orçamento não estiver aprovado, a EntityException do domínio sobe e o ExceptionMiddleware responde 400.
    public async Task<BaseResponse<ServiceOrderDto?>> RegisterRepair(long id)
    {
        var result = await _queryRepository.GetByIdForUpdateAsync(id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.ExecuteRepair();

        await _commandRepository.UpdateAsync(serviceOrder);

        _whatsAppQueue.Enqueue(new SendDeviceReadyMessageRequest(
            serviceOrder.Id.ToString(),
            serviceOrder.Customer.Name.CustomerName,
            serviceOrder.Customer.Phone.CustomerPhone));

        return new BaseResponse<ServiceOrderDto?>(
            serviceOrder.MapToServiceOrderDto(), 200, "Conserto adicionado com sucesso");
    }

    // Igual a old-handlers/AddServiceOrderDeliveryAndReturnPdfHandler.cs: salva a entrega antes do PDF;
    // OS consertada (aprovada + "Tem conserto") recebe o termo em PDF e o WhatsApp de garantia (todas as empresas).
    public async Task<BaseResponse<byte[]>> RegisterDelivery(long id)
    {
        var result = await _queryRepository.GetByIdForUpdateAsync(id);
        if (result.Data == null)
            return new BaseResponse<byte[]>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.AddDelivery();

        await _commandRepository.UpdateAsync(serviceOrder);

        if (serviceOrder.RepairStatus == ERepairStatus.Approved &&
            serviceOrder.RepairResult == ERepairResult.Repair)
        {
            var createPdf = await _pdfGenerator.GenerateCheckOutDocumentAsync(serviceOrder);
            if (!createPdf.IsSuccess)
                return new BaseResponse<byte[]>(null, 500, createPdf.Message);

            _whatsAppQueue.Enqueue(new SendGuaranteeMessageRequest(
                serviceOrder.Id.ToString(),
                serviceOrder.Customer.Name.CustomerName,
                serviceOrder.Guarantee!.ServiceOrderGuarantee,
                serviceOrder.Customer.Phone.CustomerPhone));

            return createPdf;
        }

        return new BaseResponse<byte[]>(null, 200, "Ordem de serviço entregue com sucesso!");
    }

    // Igual a old-handlers/AddProductLocationHandler.cs: normaliza (Trim + ToUpper), valida e grava a localização.
    public async Task<BaseResponse<string?>> AddProductLocation(AddProductLocationCommand command)
    {
        var normalized = command.Normalize();
        normalized.Validate();

        var result = await _queryRepository.GetByIdForUpdateAsync(normalized.ServiceOrderId);
        if (result.Data == null)
            return new BaseResponse<string?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.Product.AddLocation(normalized.Location);

        await _commandRepository.UpdateAsync(serviceOrder);

        return new BaseResponse<string?>(serviceOrder.Product.Location, 200, "Localização do produto adicionada com sucesso!");
    }

    private async Task<BaseResponse<ServiceOrderDto?>> RespondToEstimate(
        long id,
        Action<ServiceOrder> respond,
        string notificationTitle,
        string notificationMessage,
        Action<ServiceOrder> enqueueWhatsApp,
        string successMessage)
    {
        var result = await _queryRepository.GetByIdForUpdateAsync(id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        if (!serviceOrder.IsAwaitingEstimateResponse)
            return new BaseResponse<ServiceOrderDto?>(null, 409, "Esse orçamento já foi respondido.");

        respond(serviceOrder);

        await _commandRepository.UpdateAsync(serviceOrder);

        await _notificationRepository.Create(notificationTitle, notificationMessage);

        enqueueWhatsApp(serviceOrder);

        return new BaseResponse<ServiceOrderDto?>(serviceOrder.MapToServiceOrderDto(), 200, successMessage);
    }
}
