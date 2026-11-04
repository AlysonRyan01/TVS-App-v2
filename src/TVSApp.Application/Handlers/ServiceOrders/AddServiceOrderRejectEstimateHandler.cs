using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class AddServiceOrderRejectEstimateHandler : 
    IRequestHandler<AddServiceOrderRejectEstimateCommand, BaseResponse<ServiceOrderDto?>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;
    private readonly IWhatsAppQueue _whatsAppQueue;

    public AddServiceOrderRejectEstimateHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository, 
        IServiceOrderQueryRepository serviceOrderQueryRepository,
        IWhatsAppQueue whatsAppQueue)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
        _whatsAppQueue = whatsAppQueue;
    }

    public async Task<BaseResponse<ServiceOrderDto?>> Handle(
        AddServiceOrderRejectEstimateCommand request, 
        CancellationToken cancellationToken = default)
    {
        var result = await _serviceOrderQueryRepository.GetById(request.Id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.RejectEstimate();

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDto(serviceOrder);

        _whatsAppQueue.Enqueue(new SendEstimateDisapprovalAcknowledgmentRequest(
            serviceOrder.Id.ToString(),
            serviceOrder.Customer.Name.CustomerName,
            serviceOrder.Customer.Phone.CustomerPhone));

        _whatsAppQueue.Enqueue(new NotifyOwnerEstimateDecisionRequest(
            serviceOrder.Id.ToString(),
            ERepairStatus.Disapproved,
            serviceOrder.Customer.Name.CustomerName));

        return new BaseResponse<ServiceOrderDto?>(dto, 200, "Orçamento reprovado com sucesso!");
    }
}