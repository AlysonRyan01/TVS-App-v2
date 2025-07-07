using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class UpdateServiceOrderHandler 
    : IRequestHandler<UpdateServiceOrderCommand, BaseResponse<UpdateServiceOrderResponseDto?>>
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

    public async Task<BaseResponse<UpdateServiceOrderResponseDto?>> Handle(
        UpdateServiceOrderCommand request, 
        CancellationToken cancellationToken = default)
    {
        request.Validate();
        var normalized = request.Normalize();
        
        var existingCustomer = await _customerQueryRepository.GetByIdAsync(normalized.CustomerId);
        if (existingCustomer.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(
                null, 
                404, 
                $"O cliente com id: {normalized.CustomerId} não existe");

        var customer = existingCustomer.Data;

        var existingServiceOrder = await _serviceOrderQueryRepository.GetById(
            normalized.ServiceOrderId);
        if (existingServiceOrder.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(
                null, 
                404, 
                $"A ordem de serviço com id: {normalized.ServiceOrderId} não existe");

        var serviceOrder = existingServiceOrder.Data;

        serviceOrder.UpdateServiceOrder(customer,
            normalized.ProductBrand,
            normalized.ProductModel,
            normalized.ProductSerialNumber,
            normalized.ProductDefect,
            normalized.Accessories,
            normalized.ProductType,
            normalized.Enterprise);

        var result = await _serviceOrderCommandRepository.UpdateAsync(serviceOrder);
        if (!result.IsSuccess || result.Data == null)
            return new BaseResponse<UpdateServiceOrderResponseDto?>(null, 500, result.Message);
            
        var dto = ServiceOrderMapper.MapToUpdateResponseDto(result.Data);
            
        return new BaseResponse<UpdateServiceOrderResponseDto?>(dto, 200, result.Message);
    }
}