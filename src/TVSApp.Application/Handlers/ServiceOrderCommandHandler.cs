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

    public async Task<BaseResponse<CreatedServiceOrderPdf>> CreateServiceOrder(CreateServiceOrderCommand command)
    {
        command.Validate();
        var normalized = command.Normalize();

        var product = new Product(normalized.ProductBrand, normalized.ProductModel,
            normalized.ProductSerialNumber, normalized.ProductDefect, normalized.Accessories, normalized.ProductType);

        var serviceOrder = new ServiceOrder(normalized.Enterprise, normalized.CustomerId, product);

        var createServiceOrder = await _commandRepository.CreateAsync(serviceOrder);
        if (!createServiceOrder.IsSuccess)
            return new BaseResponse<CreatedServiceOrderPdf>(null, 500, createServiceOrder.Message);

        if (createServiceOrder.Data != null)
            serviceOrder = createServiceOrder.Data;

        var createPdf = await _pdfGenerator.GenerateCheckInDocumentAsync(serviceOrder);
        if (!createPdf.IsSuccess || createPdf.Data == null)
            return new BaseResponse<CreatedServiceOrderPdf>(null, 500, createPdf.Message);

        _whatsAppQueue.Enqueue(new SendWelcomeMessageRequest(
            serviceOrder.Id.ToString(),
            serviceOrder.Customer.Name.CustomerName,
            serviceOrder.Customer.Phone.CustomerPhone));

        return new BaseResponse<CreatedServiceOrderPdf>(
            new CreatedServiceOrderPdf(serviceOrder.Id, createPdf.Data), createPdf.Code, createPdf.Message);
    }

    public async Task<BaseResponse<CreatedServiceOrderPdf>> CreateSalesServiceOrder(CreateSalesServiceOrderCommand command)
    {
        command.Validate();
        var normalized = command.Normalize();

        var product = new Product(normalized.ProductBrand, normalized.ProductModel,
            normalized.ProductSerialNumber, "VENDA", "", normalized.ProductType);

        var serviceOrder = new ServiceOrder(EEnterprise.Particular, normalized.CustomerId, product);

        serviceOrder.EditGuarantee(new Guarantee(normalized.Guarantee));
        serviceOrder.EditPartCost(new PartCost(normalized.Amount));
        serviceOrder.EditServiceOrderStatus(EServiceOrderStatus.Delivered);
        serviceOrder.EditRepairResult(ERepairResult.Repair);
        serviceOrder.EditRepairStatus(ERepairStatus.Approved);
        serviceOrder.EditDeliveryDate(DateTime.UtcNow);

        var createServiceOrder = await _commandRepository.CreateAsync(serviceOrder);
        if (!createServiceOrder.IsSuccess)
            return new BaseResponse<CreatedServiceOrderPdf>(null, 500, createServiceOrder.Message);

        if (createServiceOrder.Data != null)
            serviceOrder = createServiceOrder.Data;

        var createPdf = await _pdfGenerator.GenerateSaleDocumentAsync(serviceOrder);
        if (!createPdf.IsSuccess || createPdf.Data == null)
            return new BaseResponse<CreatedServiceOrderPdf>(null, 500, createPdf.Message);

        _whatsAppQueue.Enqueue(new SendProductSaleMessageRequest(
            serviceOrder.Customer.Name.CustomerName,
            serviceOrder.Customer.Phone.CustomerPhone,
            serviceOrder.PartCost.ServiceOrderPartCost,
            serviceOrder.Guarantee!.ServiceOrderGuarantee,
            serviceOrder.Product.Type));

        return new BaseResponse<CreatedServiceOrderPdf>(
            new CreatedServiceOrderPdf(serviceOrder.Id, createPdf.Data), createPdf.Code, createPdf.Message);
    }

    public async Task<BaseResponse<byte[]>> RegeneratePdf(long id)
    {
        var result = await _queryRepository.GetByIdForUpdateAsync(id);
        if (result.Data == null)
            return new BaseResponse<byte[]>(null, 404, "Essa ordem de serviço não existe");

        var createPdf = await _pdfGenerator.RegeneratePdfAsync(result.Data);
        if (!createPdf.IsSuccess || createPdf.Data == null)
            return new BaseResponse<byte[]>(null, 500, createPdf.Message);

        return createPdf;
    }

    public async Task<BaseResponse<ServiceOrderDto?>> UpdateServiceOrder(long id, EditServiceOrderCommand command)
    {
        command.Validate();
        var normalized = command.Normalize();

        var result = await _queryRepository.GetByIdForUpdateAsync(id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;
        var isSale = serviceOrder.Product.Defect == "VENDA";

        serviceOrder.Product.UpdateProduct(
            normalized.Brand!,
            normalized.Model,
            normalized.SerialNumber,
            isSale ? "VENDA" : normalized.Defect!,
            isSale ? string.Empty : normalized.Accessories!,
            normalized.Type);
        serviceOrder.Product.AddLocation(normalized.Location!);

        serviceOrder.EditEnterprise(normalized.Enterprise);
        serviceOrder.EditEntryDate(normalized.EntryDate!.Value);
        serviceOrder.EditInspectionDate(normalized.InspectionDate);
        serviceOrder.EditResponseDate(normalized.ResponseDate);
        serviceOrder.EditRepairDate(normalized.RepairDate);
        serviceOrder.EditPurchasePartDate(normalized.PurchasePartDate);
        serviceOrder.EditDeliveryDate(normalized.DeliveryDate);
        serviceOrder.EditSolution(new Solution(normalized.Solution!));
        serviceOrder.EditGuarantee(new Guarantee(normalized.Guarantee!));
        serviceOrder.EditPartCost(new PartCost(normalized.PartCost));
        serviceOrder.EditLaborCost(new LaborCost(normalized.LaborCost));

        if (normalized.Estimate is { } estimate)
        {
            serviceOrder.EditServiceOrderStatus(estimate.ServiceOrderStatus);
            serviceOrder.EditRepairStatus(estimate.RepairStatus);
            serviceOrder.EditRepairResult(estimate.RepairResult);
            serviceOrder.EditEstimateMessage(estimate.EstimateMessage);
            serviceOrder.EditPartPurchaseCost(new PartPurchaseCost(estimate.PartPurchaseCost));
        }

        var update = await _commandRepository.UpdateAsync(serviceOrder);
        if (!update.IsSuccess)
            return new BaseResponse<ServiceOrderDto?>(null, 500, update.Message);

        return new BaseResponse<ServiceOrderDto?>(
            serviceOrder.MapToServiceOrderDto(), 200, "Ordem de serviço alterada com sucesso!");
    }

    public async Task<BaseResponse<ServiceOrderDto?>> UpdatePartPurchaseCost(long id, decimal partPurchaseCost)
    {
        var result = await _queryRepository.GetByIdForUpdateAsync(id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;
        serviceOrder.EditPartPurchaseCost(new PartPurchaseCost(partPurchaseCost));

        var update = await _commandRepository.UpdateAsync(serviceOrder);
        if (!update.IsSuccess)
            return new BaseResponse<ServiceOrderDto?>(null, 500, update.Message);

        return new BaseResponse<ServiceOrderDto?>(serviceOrder.MapToServiceOrderDto(), 200, "Valor da peça atualizado.");
    }

    public async Task<BaseResponse<ServiceOrderDto?>> ApproveEstimateByCustomer(long id, string code)
        => await CustomerCodeMatches(id, code) ? await ApproveEstimate(id) : CustomerNotFound();

    public async Task<BaseResponse<ServiceOrderDto?>> RejectEstimateByCustomer(long id, string code)
        => await CustomerCodeMatches(id, code) ? await RejectEstimate(id) : CustomerNotFound();

    private async Task<bool> CustomerCodeMatches(long id, string code)
    {
        var result = await _queryRepository.GetByIdForUpdateAsync(id);
        return result.Data != null && ServiceOrderQueryHandler.SecurityCodeMatches(result.Data.SecurityCode, code);
    }

    private static BaseResponse<ServiceOrderDto?> CustomerNotFound()
        => new(null, 404, "Essa ordem de serviço não existe");

    public async Task<BaseResponse<string?>> AddPayment(long serviceOrderId, AddPaymentCommand command)
    {
        command.Validate();

        var result = await _queryRepository.GetByIdForUpdateAsync(serviceOrderId);
        if (result.Data == null)
            return new BaseResponse<string?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;
        serviceOrder.AddPayment(new Payment(command.Amount, command.PaymentDate, serviceOrderId));

        return await SavePayments(serviceOrder, "Pagamento adicionado.");
    }

    public async Task<BaseResponse<string?>> UpdatePayment(long serviceOrderId, long paymentId, UpdatePaymentCommand command)
    {
        command.Validate();

        var result = await _queryRepository.GetByIdForUpdateAsync(serviceOrderId);
        if (result.Data == null)
            return new BaseResponse<string?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;
        if (serviceOrder.Payments.All(p => p.Id != paymentId))
            return new BaseResponse<string?>(null, 404, "Pagamento não encontrado");

        serviceOrder.UpdatePayment(paymentId, command.Amount, command.PaymentDate);

        return await SavePayments(serviceOrder, "Pagamento atualizado.");
    }

    public async Task<BaseResponse<string?>> RemovePayment(long serviceOrderId, long paymentId)
    {
        var result = await _queryRepository.GetByIdForUpdateAsync(serviceOrderId);
        if (result.Data == null)
            return new BaseResponse<string?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;
        if (serviceOrder.Payments.All(p => p.Id != paymentId))
            return new BaseResponse<string?>(null, 404, "Pagamento não encontrado");

        serviceOrder.RemovePayment(paymentId);

        return await SavePayments(serviceOrder, "Pagamento removido.");
    }

    public async Task<BaseResponse<byte[]>> GetPaymentReceipt(long serviceOrderId, long paymentId)
    {
        var result = await _queryRepository.GetByIdForUpdateAsync(serviceOrderId);
        if (result.Data == null)
            return new BaseResponse<byte[]>(null, 404, "Essa ordem de serviço não existe");

        var payment = result.Data.Payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment == null)
            return new BaseResponse<byte[]>(null, 404, "Pagamento não encontrado");

        var pdf = await _pdfGenerator.GeneratePaymentReceiptAsync(result.Data, payment);
        if (!pdf.IsSuccess || pdf.Data == null)
            return new BaseResponse<byte[]>(null, 500, pdf.Message);

        return pdf;
    }

    private async Task<BaseResponse<string?>> SavePayments(ServiceOrder serviceOrder, string message)
    {
        var update = await _commandRepository.UpdateAsync(serviceOrder);
        return update.IsSuccess
            ? new BaseResponse<string?>(message, 200, message)
            : new BaseResponse<string?>(null, 500, update.Message);
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
