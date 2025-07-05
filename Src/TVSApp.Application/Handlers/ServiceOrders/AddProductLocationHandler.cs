using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class AddProductLocationHandler : ICommandHandler<AddProductLocationCommand, BaseResponse<string?>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public AddProductLocationHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository, 
        IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }
    
    public async Task<BaseResponse<string?>> HandleAsync(AddProductLocationCommand command)
    {
        command.Normalize();
        command.Validate();

        var result = await _serviceOrderQueryRepository.GetById(command.ServiceOrderId);
        if (result.Data == null)
            return new BaseResponse<string?>(null, 404, "Essa ordem de serviço não existe");

        var serviceOrder = result.Data;

        serviceOrder.Product.AddLocation(command.Location);

        await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);

        return new BaseResponse<string?>(serviceOrder.Product.Location, 200, "Localização do produto adicionada com sucesso!");
    }
}