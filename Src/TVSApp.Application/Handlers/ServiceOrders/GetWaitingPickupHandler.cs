using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Application.Mappers;
using TVS_App.Application.Queries;
using TVS_App.Domain.Repositories.ServiceOrders;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.ServiceOrders;

public class GetWaitingPickupHandler : IQueryHandler<PaginationQuery, BaseResponse<PaginatedResult<ServiceOrderDto?>>>
{
    private readonly IServiceOrderQueryRepository _serviceOrderQueryRepository;

    public GetWaitingPickupHandler(IServiceOrderQueryRepository serviceOrderQueryRepository)
    {
        _serviceOrderQueryRepository = serviceOrderQueryRepository;
    }
    
    public async Task<BaseResponse<PaginatedResult<ServiceOrderDto?>>> HandleAsync(PaginationQuery query)
    {
        query.Validate();
        
        var repositoryResult = await _serviceOrderQueryRepository.GetWaitingPickupAsync(query.PageNumber, query.PageSize);
        if (!repositoryResult.IsSuccess || repositoryResult.Data == null)
            return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(null, 500, repositoryResult.Message);
            
        var serviceOrders = repositoryResult.Data.Items
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var dtoList = ServiceOrderMapper.MapToServiceOrderDtoList(serviceOrders);

        var paginatedResult = new PaginatedResult<ServiceOrderDto?>(
            dtoList,
            repositoryResult.Data.TotalCount,
            repositoryResult.Data.PageNumber,
            repositoryResult.Data.PageSize
        );

        return new BaseResponse<PaginatedResult<ServiceOrderDto?>>(paginatedResult, 200, "Ordens de serviço obtidas com sucesso!");
    }
}