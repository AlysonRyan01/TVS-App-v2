using MediatR;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class CreateSalesServiceOrderHandler : IRequestHandler<CreateSalesServiceOrderCommand, BaseResponse<byte[]>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IGenerateServiceOrderPdf _generateServiceOrderPdf;
    private readonly IWhatsAppQueue _whatsAppQueue;

    public CreateSalesServiceOrderHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository,
        IGenerateServiceOrderPdf generateServiceOrderPdf,
        IWhatsAppQueue whatsAppQueue)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _generateServiceOrderPdf = generateServiceOrderPdf;
        _whatsAppQueue = whatsAppQueue;
    }

    public async Task<BaseResponse<byte[]>> Handle(
        CreateSalesServiceOrderCommand request, 
        CancellationToken cancellationToken = default)
    {
        request.Validate();
        var normalized = request.Normalize();

        var product = new Product(normalized.ProductBrand, normalized.ProductModel,
            normalized.ProductSerialNumber, "VENDA", "", normalized.ProductType);

        var serviceOrder = new ServiceOrder(EEnterprise.Particular, normalized.CustomerId, product);
            
        serviceOrder.EditGuarantee(new Guarantee(normalized.Guarantee));
        serviceOrder.EditPartCost(new PartCost(normalized.Amount));
        serviceOrder.EditServiceOrderStatus(EServiceOrderStatus.Delivered);
        serviceOrder.EditRepairResult(ERepairResult.Repair);
        serviceOrder.EditRepairStatus(ERepairStatus.Approved);
        serviceOrder.EditDeliveryDate(DateTime.UtcNow);

        var createServiceOrder = await _serviceOrderCommandRepository.CreateAsync(serviceOrder);
        if (!createServiceOrder.IsSuccess)
            return new BaseResponse<byte[]>(null, 500, createServiceOrder.Message);

        if (createServiceOrder.Data != null)
            serviceOrder = createServiceOrder.Data;

        var createPdf = await _generateServiceOrderPdf.GenerateSaleDocumentAsync(serviceOrder);
        if (!createPdf.IsSuccess)
            return new BaseResponse<byte[]>(null, 500, createPdf.Message);
        
        _whatsAppQueue.Enqueue(new SendProductSaleMessageRequest(
            serviceOrder.Customer.Name.CustomerName, 
            serviceOrder.Customer.Phone.CustomerPhone, 
            serviceOrder.PartCost.ServiceOrderPartCost, 
            serviceOrder.Guarantee!.ServiceOrderGuarantee, 
            serviceOrder.Product.Type));
        
        return createPdf;
    }
}