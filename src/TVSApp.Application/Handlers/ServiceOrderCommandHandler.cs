using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Application.Handlers;

public class ServiceOrderCommandHandler : IServiceOrderCommandHandler
{
    private readonly IServiceOrderQueryRepository _queryRepository;
    private readonly IServiceOrderCommandRepository _commandRepository;
    private readonly IWhatsAppQueue _whatsAppQueue;

    public ServiceOrderCommandHandler(
        IServiceOrderQueryRepository queryRepository,
        IServiceOrderCommandRepository commandRepository,
        IWhatsAppQueue whatsAppQueue)
    {
        _queryRepository = queryRepository;
        _commandRepository = commandRepository;
        _whatsAppQueue = whatsAppQueue;
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
}
