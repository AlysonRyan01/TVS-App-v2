using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Application.Queries.ServiceOrdersQueries;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetServiceOrdersByDateHandler : IQueryHandler<GetServiceOrdersByDateQuery, BaseResponse<List<ServiceOrderDto>>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetServiceOrdersByDateHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }
    
    public async Task<BaseResponse<List<ServiceOrderDto>>> HandleAsync(GetServiceOrdersByDateQuery query)
    {
        var repositoryResult = await _serviceOrderQueryRepository.GetServiceOrdersByDate(query.StartDate, query.EndDate);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<List<ServiceOrderDto>>(null, 500, repositoryResult.Message);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDtoList(repositoryResult.Data);
            
        return new BaseResponse<List<ServiceOrderDto>>(dto, 200, "Ordens de serviço obtidas com sucesso!");
    }
}