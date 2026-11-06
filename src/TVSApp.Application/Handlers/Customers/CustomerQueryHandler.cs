using TVS_App.Application.DTOs;
using TVS_App.Application.Mappers;
using TVS_App.Application.Requests.CustomerRequests.CustomerQueries;
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

    /// <summary>Grade de clientes: filtros combinados (nome, telefone, CPF/CNPJ), mais recentes primeiro.</summary>
    public async Task<BaseResponse<PaginatedResult<CustomerDto>>> Search(CustomerSearchQuery query)
    {
        var name = Clean(query.Name);
        if (name is { Length: < 3 })
            return BadSearch("Digite pelo menos 3 letras do nome.");

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(query.Phone))
        {
            phone = new string(query.Phone.Where(char.IsDigit).ToArray());
            if (phone.Length < 4)
                return BadSearch("Digite pelo menos 4 números do telefone.");
        }

        var document = Clean(query.Document)?
            .Replace(".", "").Replace("-", "").Replace("/", "").Replace(" ", "")
            .ToUpperInvariant();
        if (document is { Length: < 3 })
            return BadSearch("Digite pelo menos 3 caracteres do CPF/CNPJ.");

        if (query.PageNumber < 1 || query.PageSize is < 1 or > 100)
            return BadSearch("Paginação inválida.");

        var result = await _customerQueryRepository.SearchAsync(
            new CustomerSearchFilter(name, phone, document), query.PageNumber, query.PageSize);

        if (!result.IsSuccess || result.Data is null)
            return new BaseResponse<PaginatedResult<CustomerDto>>(null, result.Code, result.Message);

        var page = new PaginatedResult<CustomerDto>(
            CustomerMapper.MapToDtoList(result.Data.Items),
            result.Data.TotalCount,
            result.Data.PageNumber,
            result.Data.PageSize,
            result.Data.TotalPages);

        var message = page.TotalCount switch
        {
            0 => "Nenhum cliente encontrado.",
            1 => "1 cliente encontrado.",
            var n => $"{n} clientes encontrados."
        };
        return new BaseResponse<PaginatedResult<CustomerDto>>(page, 200, message);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BaseResponse<PaginatedResult<CustomerDto>> BadSearch(string message) => new(null, 400, message);
}
