using MediatR;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class AddServiceOrderDeliveryAndReturnPdfHandler : IRequestHandler<AddServiceOrderDeliveryAndReturnPdfCommand, BaseResponse<byte[]>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;
    private readonly IWhatsAppQueue _whatsappQueue;
    private readonly IGenerateServiceOrderPdf _generateServiceOrderPdf;

    public AddServiceOrderDeliveryAndReturnPdfHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository, 
        IServiceOrderQueryRepository serviceOrderQueryRepository,
        IWhatsAppQueue whatsAppQueue,
        IGenerateServiceOrderPdf generateServiceOrderPdf)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
        _whatsappQueue = whatsAppQueue;
        _generateServiceOrderPdf = generateServiceOrderPdf;
    }

    public async Task<BaseResponse<byte[]>> Handle(
        AddServiceOrderDeliveryAndReturnPdfCommand request, 
        CancellationToken cancellationToken = default)
    {
        var result = await _serviceOrderQueryRepository.GetById(request.Id);
        if (result.Data == null)
            return new BaseResponse<byte[]>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.AddDelivery();

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);

        if (serviceOrder.RepairStatus == ERepairStatus.Approved &&
            serviceOrder.RepairResult == ERepairResult.Repair)
        {
            var createPdf = await _generateServiceOrderPdf.GenerateCheckOutDocumentAsync(serviceOrder);
            if (!createPdf.IsSuccess)
                return new BaseResponse<byte[]>(null, 500, createPdf.Message);
            
            _whatsappQueue.Enqueue(new SendGuaranteeMessageRequest(
                serviceOrder.Id.ToString(),
                serviceOrder.Customer.Name.CustomerName,
                serviceOrder.Guarantee!.ServiceOrderGuarantee,
                serviceOrder.Customer.Phone.CustomerPhone));
                
            return createPdf;
        }

        return new BaseResponse<byte[]>(null, 200, "Ordem de serviço entregue com sucesso!");
    }
}