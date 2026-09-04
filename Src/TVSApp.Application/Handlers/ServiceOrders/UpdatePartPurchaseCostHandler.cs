using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class UpdatePartPurchaseCostHandler : IRequestHandler<UpdatePartPurchaseCostCommand, BaseResponse<ServiceOrderDto?>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public UpdatePartPurchaseCostHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository,
        IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<ServiceOrderDto?>> Handle(
        UpdatePartPurchaseCostCommand request,
        CancellationToken cancellationToken = default)
    {
        var result = await _serviceOrderQueryRepository.GetById(request.Id);
        if (result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.EditPartPurchaseCost(new PartPurchaseCost(request.PartPurchaseCost));

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);

        var dto = ServiceOrderMapper.MapToServiceOrderDto(serviceOrder);

        return new BaseResponse<ServiceOrderDto?>(dto, 200, "Valor da peça atualizado com sucesso!");
    }
}
