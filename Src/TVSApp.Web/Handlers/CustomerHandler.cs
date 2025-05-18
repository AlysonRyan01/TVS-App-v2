using System.Net.Http.Json;
using TVS_App.Application.Commands;
using TVS_App.Application.Commands.CustomerCommands;
using TVS_App.Application.DTOs;
using TVS_App.Domain.Entities;
using TVSApp.Web.Exceptions;
using Exception = System.Exception;

namespace TVSApp.Web.Handlers;

public class CustomerHandler
{
    private readonly HttpClient _httpClient;
    
    public CustomerHandler(IHttpClientFactory clientFactory)
    {
        _httpClient = clientFactory.CreateClient("api");
    }

    public async Task<BaseResponse<Customer?>> CreateCustomerAsync(CreateCustomerCommand command)
    {
        try
        {
            command.Validate();
            var response = await _httpClient.PostAsJsonAsync("create-customer", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<Customer?>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }
            
            var content = await response.Content.ReadFromJsonAsync<BaseResponse<Customer?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<Customer?>(null, 500, content?.Message);

            return content;
        }
        catch (Exception ex)
        {
            return ExceptionHandler.Handle<Customer?>(ex);
        }
    }

    public async Task<BaseResponse<Customer?>> UpdateCustomerAsync(UpdateCustomerCommand command)
    {
        try
        {
            command.Validate();
            var response = await _httpClient.PutAsJsonAsync("update-customer", command);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new BaseResponse<Customer?>(null, (int)response.StatusCode, 
                    $"Erro na API: {response.StatusCode}. Detalhes: {errorContent}");
            }
            
            var content = await response.Content.ReadFromJsonAsync<BaseResponse<Customer?>>();
            if (content == null || !content.IsSuccess)
                return new BaseResponse<Customer?>(null, 500, content?.Message);
            
            return content;
        }
        catch (Exception ex)
        {
            return ExceptionHandler.Handle<Customer?>(ex);
        }
    }

    public async Task<BaseResponse<Customer?>> GetCustomerByIdAsync(GetCustomerByIdCommand command)
    {
        try
        {
            command.Validate();
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<Customer?>>(
                $"get-customer-by-id/{command.Id}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<Customer?>(null, 500, response?.Message);
            
            return response;
        }
        catch (Exception ex)
        {
            return ExceptionHandler.Handle<Customer?>(ex);
        }
    }
    
    public async Task<BaseResponse<List<Customer>>> GetCustomerByNameAsync(string name)
    {
        try
        {
            if (string.IsNullOrEmpty(name))
                return new BaseResponse<List<Customer>>(null, 400, "A nome não pode ser vazio");
            
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<List<Customer>>>(
                $"get-customer-by-name?name={Uri.EscapeDataString(name)}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<List<Customer>>(null, 500, response?.Message);
            
            return response;
        }
        catch (Exception ex)
        {
            return ExceptionHandler.Handle<List<Customer>>(ex);
        }
    }

    public async Task<BaseResponse<PaginatedResult<Customer?>>> GetAllCustomersAsync(PaginationCommand command)
    {
        try
        {
            command.Validate();
            var response = await _httpClient.GetFromJsonAsync<BaseResponse<PaginatedResult<Customer?>>>(
                $"get-all-customers/{command.PageSize}/{command.PageNumber}");
            
            if (response == null || !response.IsSuccess)
                return new BaseResponse<PaginatedResult<Customer?>>(null, 500, response?.Message);
            
            return response;
        }
        catch (Exception ex)
        {
            return ExceptionHandler.Handle<PaginatedResult<Customer?>>(ex);
        }
    }
}