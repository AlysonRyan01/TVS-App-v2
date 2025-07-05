using TVS_App.Application.Commands.ServiceOrderCommands;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class UpdateServiceOrderHandler : ICommandHandler<UpdateServiceOrderCommand, BaseResponse<UpdateServiceOrderResponseDto?>>
{
    private readonly IServiceOrderCommandRepository _serviceOrderCommandRepository;
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;
    private readonly ICustomerQueryRepository _customerQueryRepository;

    public UpdateServiceOrderHandler(
        IServiceOrderCommandRepository serviceOrderCommandRepository,
        ICustomerQueryRepository customerQueryRepository,
        IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderCommandRepository = serviceOrderCommandRepository;
        _customerQueryRepository = customerQueryRepository;
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<UpdateServiceOrderResponseDto?>> HandleAsync(UpdateServiceOrderCommand command)
    {
        command.Normalize();
        command.Validate();

        var existingCustomer = await _customerQueryRepository.GetByIdAsync(command.CustomerId);
        if (existingCustomer.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 404, $"O cliente com id: {command.CustomerId} não existe");

        var customer = existingCustomer.Data;

        var existingServiceOrder = await _serviceOrderQueryRepository.GetById(command.ServiceOrderId);
        if (existingServiceOrder.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 404, $"A ordem de serviço com id: {command.ServiceOrderId} não existe");

        var serviceOrder = existingServiceOrder.Data;

        serviceOrder.UpdateServiceOrder(customer,
            command.ProductBrand,
            command.ProductModel,
            command.ProductSerialNumber,
            command.ProductDefect,
            command.Accessories,
            command.ProductType,
            command.Enterprise);

        var result = await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
        if (!result.IsSuccess || result.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 500, result.Message);
            
        var dto = ServiceOrderMapper.MapToUpdateResponseDto(result.Data);
            
        return new BaseResponse<UpdateServiceOrderResponseDto?>(dto, 200, result.Message);
    }
}