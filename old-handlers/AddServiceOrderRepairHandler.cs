using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class AddServiceOrderRepairHandler : IRequestHandler<AddServiceOrderRepairCommand, BaseResponse<ServiceOrderDto?>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;
    private readonly IWhatsAppQueue _whatsAppQueue;

    public AddServiceOrderRepairHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository, 
        IServiceOrderQueryRepository serviceOrderQueryRepository,
        IWhatsAppQueue whatsAppQueue)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
        _whatsAppQueue = whatsAppQueue;
    }

    public async Task<BaseResponse<ServiceOrderDto?>> Handle(
        AddServiceOrderRepairCommand request, 
        CancellationToken cancellationToken = default)
    {
        var result = await _serviceOrderQueryRepository.GetById(request.Id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.ExecuteRepair();

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDto(serviceOrder);
        
        _whatsAppQueue.Enqueue(new SendDeviceReadyMessageRequest(
            serviceOrder.Id.ToString(),
            serviceOrder.Customer.Name.CustomerName,
            serviceOrder.Customer.Phone.CustomerPhone));
        
        return new BaseResponse<ServiceOrderDto?>(dto, 200, "Conserto adicionado com sucesso");
    }
}