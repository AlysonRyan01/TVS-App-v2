using TVS_App.Application.DTOs;
using TVS_App.Application.Mappers;
using TVS_App.Application.Queries.CustomersQueries;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public class GetCustomerByIdHandler : IQueryHandler<GetCustomerByIdQuery, BaseResponse<CustomerDto?>>
{
    private readonly ICustomerQueryRepository _customerQueryRepository;

    public GetCustomerByIdHandler(ICustomerQueryRepository customerQueryRepository)
    {
        _customerQueryRepository = customerQueryRepository;
    }

    public async Task<BaseResponse<CustomerDto?>> HandleAsync(GetCustomerByIdQuery query)
    {
        query.Validate();

        var response = await _customerQueryRepository.GetByIdAsync(query.Id);
            
        if (!response.IsSuccess || response.Data == null)
            return new BaseResponse<CustomerDto?>(null, response.Code, response.Message);
            
        var dto = CustomerMapper.MapToDto(response.Data);
            
        return new BaseResponse<CustomerDto?>(dto, 200, "Cliente obtido com sucesso!");
    }
}