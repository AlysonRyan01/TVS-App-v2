using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class CreateSalesServiceOrderHandler : ICommandHandler<CreateSalesServiceOrderCommand, BaseResponse<byte[]>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IGenerateServiceOrderPdf _generateServiceOrderPdf;
    private readonly IWhatsappService _whatsappService;

    public CreateSalesServiceOrderHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository,
        IGenerateServiceOrderPdf generateServiceOrderPdf,
        IWhatsappService whatsappService)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _generateServiceOrderPdf = generateServiceOrderPdf;
        _whatsappService = whatsappService;
    }
    
    public async Task<BaseResponse<byte[]>> HandleAsync(CreateSalesServiceOrderCommand command)
    {
        command.Normalize();
        command.Validate();

        var product = new Product(command.ProductBrand, command.ProductModel,
            command.ProductSerialNumber, "VENDA", "", command.ProductType);

        var serviceOrder = new ServiceOrder(EEnterprise.Particular, command.CustomerId, product);
            
        serviceOrder.EditGuarantee(new Guarantee(command.Guarantee));
        serviceOrder.EditPartCost(new PartCost(command.Amount));
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
            
        await _whatsappService.SendProductSaleMessage(
            serviceOrder.Customer.Name.CustomerName, 
            serviceOrder.Customer.Phone.CustomerPhone, 
            serviceOrder.PartCost.ServiceOrderPartCost, 
            serviceOrder.Guarantee!.ServiceOrderGuarantee, 
            serviceOrder.Product.Type);

        return createPdf;
    }
}