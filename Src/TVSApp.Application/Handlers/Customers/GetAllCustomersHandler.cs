using MediatR;
using TVS_App.Application.DTOs;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.CustomerRequests.CustomersQueries;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public class GetAllCustomersHandler : IRequestHandler<GetAllCustomersQuery, BaseResponse<PaginatedResult<CustomerDto>>>
{
    private readonly ICustomerQueryRepository _customerQueryRepository;

    public GetAllCustomersHandler(ICustomerQueryRepository customerQueryRepository)
    {
        _customerQueryRepository = customerQueryRepository;
    }
    
    public async Task<BaseResponse<PaginatedResult<CustomerDto>>> Handle(
        GetAllCustomersQuery request, 
        CancellationToken cancellationToken = default)
    {
        request.Validate();

        var response = await _customerQueryRepository.GetAllAsync(request.PageNumber, request.PageSize);
            
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