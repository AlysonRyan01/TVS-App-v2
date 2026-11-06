using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TVS_App.Application.DTOs;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Domain.Shared;

namespace TVS_App.Infrastructure.Services.AddressService;

public class AddressService : IAddressService
{
    private readonly HttpClient _httpClient;
    
    public AddressService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<BaseResponse<AddressDto>> GetAddressByCep(string cep)
    {
        try
        {
            var formattedCep = FormatCep(cep);
            
            var url = $"https://viacep.com.br/ws/{formattedCep}/json/";
            
            var response = await _httpClient.GetFromJsonAsync<ApiResponse>(url);

            if (response == null)
                return new BaseResponse<AddressDto>(null, 500, "Erro ao buscar localidade por CEP");

            // O ViaCEP responde {"erro": true} para CEP inexistente.
            if (response.Erro == true)
                return new BaseResponse<AddressDto>(null, 404, "CEP não encontrado.");

            var dto = new AddressDto(
                response.Cep ?? string.Empty,
                response.Logradouro ?? string.Empty,
                response.Bairro ?? string.Empty,
                response.Localidade ?? string.Empty,
                response.Uf ?? string.Empty);
            
            return new BaseResponse<AddressDto>(dto, 200, "Sucesso ao obter o endereço");
        }
        catch (HttpRequestException httpEx)
        {
            return new BaseResponse<AddressDto>(null, 500, httpEx.Message);
        }
        catch (TaskCanceledException taskCanceledEx)
        {
            return new BaseResponse<AddressDto>(null, 500, $"Ocorreu um erro: {taskCanceledEx.Message}");
        }
        catch (Exception ex)
        {
            return new BaseResponse<AddressDto>(null, 500, $"Ocorreu um erro: {ex.Message}");
        }
    }
    
    public static string FormatCep(string cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
            return cep;

        return Regex.Replace(cep, @"\D", "");
    }

    private record ApiResponse(
        [property: JsonPropertyName("cep")] string? Cep,
        [property: JsonPropertyName("logradouro")] string? Logradouro,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("localidade")] string? Localidade,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("erro")] bool? Erro
    );
}