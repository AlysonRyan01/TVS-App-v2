using TVS_App.Application.DTOs;
using TVS_App.Application.Mappers;
using TVS_App.Domain.Repositories.Customers;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Customers;

public class CustomerQueryHandler : ICustomerQueryHandler
{
    public const int SearchLimit = 21;

    private readonly ICustomerQueryRepository _customerQueryRepository;

    public CustomerQueryHandler(ICustomerQueryRepository customerQueryRepository)
    {
        _customerQueryRepository = customerQueryRepository;
    }

    public async Task<BaseResponse<List<CustomerDto>>> SearchByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 3)
            return new BaseResponse<List<CustomerDto>>(null, 400, "Digite pelo menos 3 letras.");

        var customersResponse = await _customerQueryRepository.GetCustomerByName(name, SearchLimit);

        if (!customersResponse.IsSuccess || customersResponse.Data == null)
            return new BaseResponse<List<CustomerDto>>(null, customersResponse.Code, customersResponse.Message);

        var dto = CustomerMapper.MapToDtoList(customersResponse.Data);

        return new BaseResponse<List<CustomerDto>>(dto, 200, customersResponse.Message);
    }
}
