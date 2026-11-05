using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrdersQueries;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetServiceOrdersBySerialNumberHandler 
    : IRequestHandler<GetServiceOrdersBySerialNumberQuery, BaseResponse<List<ServiceOrderDto>>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetServiceOrdersBySerialNumberHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<List<ServiceOrderDto>>> Handle(
        GetServiceOrdersBySerialNumberQuery request, 
        CancellationToken cancellationToken = default)
    {
        request.Validate();

        var repositoryResult = await _serviceOrderQueryRepository.GetServiceOrdersBySerialNumber(
            request.NormalizedSerialNumber);
        
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, repositoryResult.Message);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDtoList(repositoryResult.Data);
            
        return new BaseResponse<List<ServiceOrderDto>>(dto, 200, "Ordens de serviço obtidas com sucesso!");
    }
}