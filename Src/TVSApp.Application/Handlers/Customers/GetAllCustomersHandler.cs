using TVS_App.Application.DTOs;
using TVS_App.Application.Mappers;
using TVS_App.Application.Queries;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public class GetAllCustomersHandler : IQueryHandler<PaginationQuery, BaseResponse<PaginatedResult<CustomerDto>>>
{
    private readonly ICustomerQueryRepository _customerQueryRepository;

    public GetAllCustomersHandler(ICustomerQueryRepository customerQueryRepository)
    {
        _customerQueryRepository = customerQueryRepository;
    }
    
    public async Task<BaseResponse<PaginatedResult<CustomerDto>>> HandleAsync(PaginationQuery query)
    {
        query.Validate();

        var response = await _customerQueryRepository.GetAllAsync(query.PageNumber, query.PageSize);
            
        if (!response.IsSuccess || response.Data == null || !response.Data.Items.Any())
            return new BaseResponse<PaginatedResult<CustomerDto>>(null, 404, response.Message);
            
        var dto = CustomerMapper.MapToDtoList(response.Data.Items!);
            
        var paginatedResult = new PaginatedResult<CustomerDto>(
            dto,
            response.Data.TotalCount,
            response.Data.PageNumber,
            response.Data.PageSize,
            response.Data.TotalPages);
            
        return new BaseResponse<PaginatedResult<CustomerDto>>(paginatedResult, 200, "Clientes obtidos com sucesso!");
    }
}