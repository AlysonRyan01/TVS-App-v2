using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Application.Queries.ServiceOrdersQueries;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetServiceOrderByIdHandler : IQueryHandler<GetServiceOrderByIdQuery, BaseResponse<ServiceOrderDto?>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetServiceOrderByIdHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }

    public async Task<BaseResponse<ServiceOrderDto?>> HandleAsync(GetServiceOrderByIdQuery query)
    {
        query.Validate();

        var result =  await _serviceOrderQueryRepository.GetById(query.Id);
            
        if (!result.IsSuccess || result.Data == null)
            return new BaseResponse<ServiceOrderDto?>(null, 500, result.Message);
            
        var dto = ServiceOrderMapper.MapToServiceOrderDto(result.Data);
            
        return new BaseResponse<ServiceOrderDto?>(dto, 200, result.Message);
    }
}