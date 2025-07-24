using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class AddServiceOrderEstimateHandler : IRequestHandler<AddServiceOrderEstimateCommand, BaseResponse<ServiceOrderDto?>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;
    private readonly IWhatsappService _whatsappService;

    public AddServiceOrderEstimateHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository, 
        IServiceOrderQueryRepository serviceOrderQueryRepository,
        IWhatsappService whatsappService)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
        _whatsappService = whatsappService;
    }

    public async Task<BaseResponse<ServiceOrderDto?>> Handle(
        AddServiceOrderEstimateCommand request, 
        CancellationToken cancellationToken = default)
    {
        var normalized = request.Normalize();
        normalized.Validate();

        var result = await _serviceOrderQueryRepository.GetById(normalized.ServiceOrderId);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.AddEstimate(
            normalized.Solution, 
            normalized.Guarantee, 
            normalized.PartCost, 
            normalized.LaborCost, 
            normalized.RepairResult, 
            normalized.EstimateMessage);

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);

        var message = "Orçamento adicionado com sucesso!";

        if (serviceOrder.Enterprise != EEnterprise.PhilcoBritania)
        {
            var whatsappResult = await _whatsappService.SendEstimateMessage(
                serviceOrder.Id.ToString(),
                serviceOrder.SecurityCode,
                serviceOrder.RepairResult!.Value,
                serviceOrder.Customer.Name.CustomerName,
                normalized.EstimateMessage.ToUpper(),
                serviceOrder.Customer.Phone.CustomerPhone);
            
            message = whatsappResult.IsSuccess
                ? "Orçamento adicionado e mensagem enviada com sucesso!"
                : "Orçamento adicionado, mas falha ao enviar mensagem";
        }
            
        var dto = ServiceOrderMapper.MapToServiceOrderDto(serviceOrder);

        return new BaseResponse<ServiceOrderDto?>(dto, 200, message);
    }
}