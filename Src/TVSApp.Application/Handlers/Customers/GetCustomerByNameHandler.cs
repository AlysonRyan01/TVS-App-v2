using TVS_App.Application.DTOs;
using TVS_App.Application.Mappers;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public class GetCustomerByNameHandler : IQueryHandler<string, BaseResponse<List<CustomerDto>>>
{
    private readonly ICustomerQueryRepository _customerQueryRepository;

    public GetCustomerByNameHandler(ICustomerQueryRepository customerQueryRepository)
    {
        _customerQueryRepository = customerQueryRepository;
    }

    public async Task<BaseResponse<List<CustomerDto>>> HandleAsync(string name)
    {
        var customersResponse = await _customerQueryRepository.GetCustomerByName(name);
        
        if (!customersResponse.IsSuccess || customersResponse.Data == null)
            return new BaseResponse<List<CustomerDto>>(null, 404, customersResponse.Message);
        
        var dto = CustomerMapper.MapToDtoList(customersResponse.Data);
        
        return new BaseResponse<List<CustomerDto>>(dto, 200, customersResponse.Message);
    }
}