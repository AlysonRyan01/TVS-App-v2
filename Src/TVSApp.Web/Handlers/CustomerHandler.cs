using System.Net.Http.Json;
using TVS_App.Application.Commands;
using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.DTOs;
using TVS_App.Domain.Shared;

namespace TVSApp.Web.Handlers;

public class CustomerHandler
{
    private readonly HttpClient _httpClient;
    
    public CustomerHandler(IHttpClientFactory clientFactory)
    {
        _httpClient = clientFactory.CreateClient("api");
    }

    public async Task<BaseResponse<CustomerDto?>> CreateCustomerAsync(CreateCustomerCommand command)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("create-customer", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<CustomerDto?>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }
            
            var content = await response.Content.ReadFromJsonAsync<BaseResponse<CustomerDto?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<CustomerDto?>(null, 500, content?.Message);

            return content;
        }
        catch
        {
            return new BaseResponse<CustomerDto?>(null, 500, "Erro ao se conectar com o servidor.");
        }
    }

    public async Task<BaseResponse<CustomerDto?>> UpdateCustomerAsync(UpdateCustomerCommand command)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("update-customer", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<CustomerDto?>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }
            
            var content = await response.Content.ReadFromJsonAsync<BaseResponse<CustomerDto?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<CustomerDto?>(null, 500, content?.Message);
            
            return content;
        }
        catch
        {
            return new BaseResponse<CustomerDto?>(null, 500, "Erro ao se conectar com o servidor.");
        }
    }

    public async Task<BaseResponse<CustomerDto?>> GetCustomerByIdAsync(GetCustomerByIdCommand command)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<CustomerDto?>>(
                $"get-customer-by-id/{command.Id}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<CustomerDto?>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<CustomerDto?>(null, 500, "Erro ao se conectar com o servidor.");
        }
    }
    
    public async Task<BaseResponse<List<CustomerDto>>> GetCustomerByNameAsync(string name)
    {
        try
        {
            if (string.IsNullOrEmpty(name))
                return new BaseResponse<List<CustomerDto>>(null, 400, "A nome não pode ser vazio");
            
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<List<CustomerDto>>>(
                $"get-customer-by-name?name={Uri.EscapeDataString(name)}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<List<CustomerDto>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<List<CustomerDto>>(null, 500, "Erro ao se conectar com o servidor.");
        }
    }

    public async Task<BaseResponse<PaginatedResult<CustomerDto?>>> GetAllCustomersAsync(PaginationCommand command)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<PaginatedResult<CustomerDto?>>>(
                $"get-all-customers/{command.PageSize}/{command.PageNumber}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<PaginatedResult<CustomerDto?>>(null, 500, response?.Message);
            
            return response;
        }
        catch
        {
            return new BaseResponse<PaginatedResult<CustomerDto?>>(null, 500, "Erro ao se conectar com o servidor.");
        }
    }
}