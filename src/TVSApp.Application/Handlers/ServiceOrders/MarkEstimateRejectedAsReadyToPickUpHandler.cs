using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class MarkEstimateRejectedAsReadyToPickUpHandler : 
    IRequestHandler<MarkEstimateRejectedAsReadyToPickUpCommand, BaseResponse<ServiceOrderDto>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IWhatsAppQueue _whatsAppQueue;

    public MarkEstimateRejectedAsReadyToPickUpHandler(
        IServiceOrderQueryRepository serviceOrderQueryRepository, 
        IServiceOrderCommandRepository serviceOrderCommandRepository,
        IWhatsAppQueue whatsAppQueue)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _whatsAppQueue = whatsAppQueue;
    }

    public async Task<BaseResponse<ServiceOrderDto>> Handle(
        MarkEstimateRejectedAsReadyToPickUpCommand request, 
        CancellationToken cancellationToken = default)
    {
        if (request.ServiceOrderId == 0)
            return new BaseResponse<ServiceOrderDto>(
                null, 400, "O número da ordem de serviço não pode ser 0");
        
        var serviceOrderResult = await _serviceOrderQueryRepository.GetById(
            request.ServiceOrderId);
        
        if (!serviceOrderResult.IsSuccess || serviceOrderResult.Data == null)
            return new BaseResponse<ServiceOrderDto>(null, 404, serviceOrderResult.Message);
        
        var serviceOrder = serviceOrderResult.Data;
        
        serviceOrder.MarkEstimateRejectedAsReadyToPickUp();
        
        var updateResult = await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
        
        var dto = ServiceOrderMapper.MapToServiceOrderDto(serviceOrder);

        if (!updateResult.IsSuccess)
            return new BaseResponse<ServiceOrderDto>(null, updateResult.Code, updateResult.Message);

        _whatsAppQueue.Enqueue(new SendRejectionReadyForPickupMessageRequest(
            serviceOrder.Customer.Name.CustomerName,
            serviceOrder.Customer.Phone.CustomerPhone));
        
        return new BaseResponse<ServiceOrderDto>(dto, 200, "Ordem de serviço marcada como pronta para entrega");
    }
}