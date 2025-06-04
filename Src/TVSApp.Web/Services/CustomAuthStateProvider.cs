using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using TVS_App.Application.Requests.AuthRequests;
using TVS_App.Domain.Shared;

namespace TVSApp.Web.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ISyncLocalStorageService _localStorage;

    public CustomAuthStateProvider(IHttpClientFactory httpClientFactory, ISyncLocalStorageService localStorageService)
    {
        _httpClient = httpClientFactory.CreateClient("api");
        _localStorage = localStorageService;

        var token = _localStorage.GetItem<string>("jwtToken");
        if (!string.IsNullOrEmpty(token) && !IsTokenExpired(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _localStorage.GetItem<string>("jwtToken");
        
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
        {
            _localStorage.RemoveItem("jwtToken");
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return Task.FromResult(new AuthenticationState(anonymous));
        }
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(user));
    }

    public async Task<BaseResponse<string>> Login(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<BaseResponse<string>>();
                var errorMessage = errorResponse?.Message ?? "Falha no login";
                return new BaseResponse<string>(errorMessage, (int)response.StatusCode, errorMessage);
            }

            var baseResponse = await response.Content.ReadFromJsonAsync<BaseResponse<string>>();
            if (baseResponse == null || string.IsNullOrWhiteSpace(baseResponse.Data))
                return new BaseResponse<string>("Token inválido", 500, "Resposta de login inválida");

            var jwtToken = baseResponse.Data;
            
            _localStorage.SetItem("jwtToken", jwtToken);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

            return new BaseResponse<string>("Login realizado", 200, "Autenticação bem-sucedida");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new BaseResponse<string>("Credenciais inválidas", 401, "Usuário ou senha incorretos");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return new BaseResponse<string>("Muitas tentativas", 429, "Muitas tentativas de login. Tente novamente mais tarde");
        }
        catch (HttpRequestException ex)
        {
            return new BaseResponse<string>("Serviço indisponível", 503, $"Serviço de autenticação offline: {ex.Message}");
        }
        catch (JsonException)
        {
            return new BaseResponse<string>("Formato inválido", 500, "Resposta de login em formato inesperado");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return new BaseResponse<string>("Tempo esgotado", 504, "Serviço de autenticação não respondeu a tempo");
        }
        catch (Exception ex)
        {
            return new BaseResponse<string>("Falha no login", 500, $"Erro durante o login: {ex.Message}");
        }
    }

    private static bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}
