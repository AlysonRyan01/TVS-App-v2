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

public class AddServiceOrderApproveEstimateHandler : IRequestHandler<AddServiceOrderApproveEstimateCommand, BaseResponse<ServiceOrderDto?>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;
    private readonly IWhatsAppQueue _whatsappQueue;

    public AddServiceOrderApproveEstimateHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository, 
        IServiceOrderQueryRepository serviceOrderQueryRepository,
        IWhatsAppQueue whatsappQueue)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
        _whatsappQueue = whatsappQueue;
    }

    public async Task<BaseResponse<ServiceOrderDto?>> Handle(
        AddServiceOrderApproveEstimateCommand request, 
        CancellationToken cancellationToken = default)
    {
        var result = await _serviceOrderQueryRepository.GetById(request.Id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.ApproveEstimate();

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDto(serviceOrder);

        if (serviceOrder.Enterprise != EEnterprise.PhilcoBritania)
        {
            _whatsappQueue.Enqueue(new SendThankYouForEstimateApprovalRequest(
                serviceOrder.Id.ToString(),
                serviceOrder.Customer.Name.CustomerName,
                serviceOrder.Customer.Phone.CustomerPhone,
                serviceOrder.TotalAmount));
        
            _whatsappQueue.Enqueue(new NotifyOwnerEstimateDecisionRequest(
                serviceOrder.Id.ToString(), 
                ERepairStatus.Approved,
                serviceOrder.Customer.Name.CustomerName));
        }
        
        return new BaseResponse<ServiceOrderDto?>(dto, 200, "Aprovação adicionada com sucesso!");
    }
}