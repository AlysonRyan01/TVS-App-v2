using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetServiceOrdersByCustomerNameHandler : IQueryHandler<string, BaseResponse<List<ServiceOrderDto>>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetServiceOrdersByCustomerNameHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<List<ServiceOrderDto>>> HandleAsync(string customerName)
    {
        var repositoryResult = await _serviceOrderQueryRepository.GetServiceOrdersByCustomerName(customerName);
        if (!repositoryResult.IsSuccess)
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data;
            
        var dto = ServiceOrderMapper.MapToServiceOrderDtoList(serviceOrders ?? new List<ServiceOrder>());
            
        return new BaseResponse<List<ServiceOrderDto>>(dto, 200, "Ordens de serviço obtidas com sucesso!");
    }
}