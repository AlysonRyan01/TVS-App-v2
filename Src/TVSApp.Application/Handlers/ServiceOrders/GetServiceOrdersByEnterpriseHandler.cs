using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetServiceOrdersByEnterpriseHandler 
    : IRequestHandler<GetServiceOrdersByEnterpriseQuery, BaseResponse<List<ServiceOrderDto>>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetServiceOrdersByEnterpriseHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<List<ServiceOrderDto>>> Handle(
        GetServiceOrdersByEnterpriseQuery request, 
        CancellationToken cancellationToken = default)
    {
        var repositoryResult = await _serviceOrderQueryRepository.GetServiceOrdersByEnterprise(
            request.Enterprise);
        
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, repositoryResult.Message);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDtoList(repositoryResult.Data);
            
        return new BaseResponse<List<ServiceOrderDto>>(dto, 200, "Ordens de serviço obtidas com sucesso!");
    }
}