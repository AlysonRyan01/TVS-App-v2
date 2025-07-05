using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class CreateServiceOrderAndReturnPdfHandler : ICommandHandler<CreateServiceOrderCommand, BaseResponse<byte[]>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IGenerateServiceOrderPdf _generateServiceOrderPdf;
    private readonly IWhatsappService _whatsappService;

    public CreateServiceOrderAndReturnPdfHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository,
        IGenerateServiceOrderPdf generateServiceOrderPdf,
        IWhatsappService whatsappService)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _generateServiceOrderPdf = generateServiceOrderPdf;
        _whatsappService = whatsappService;
    }

    public async Task<BaseResponse<byte[]>> HandleAsync(CreateServiceOrderCommand command)
    {
        command.Normalize();
        command.Validate();

        var product = new Product(command.ProductBrand, command.ProductModel,
            command.ProductSerialNumber, command.ProductDefect, command.Accessories, command.ProductType);

        var serviceOrder = new ServiceOrder(command.Enterprise, command.CustomerId, product);

        var createServiceOrder = await _serviceOrderCommandRepository.CreateAsync(serviceOrder);
        if (!createServiceOrder.IsSuccess)
            return new BaseResponse<byte[]>(null, 500, createServiceOrder.Message);

        if (createServiceOrder.Data != null)
            serviceOrder = createServiceOrder.Data;

        var createPdf = await _generateServiceOrderPdf.GenerateCheckInDocumentAsync(serviceOrder);
        if (!createPdf.IsSuccess)
            return new BaseResponse<byte[]>(null, 500, createPdf.Message);
            
        await _whatsappService.SendWelcomeMessage(
            serviceOrder.Id.ToString(), 
            serviceOrder.Customer.Name.CustomerName, 
            serviceOrder.Customer.Phone.CustomerPhone);

        return createPdf;
    }
}