using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using TVS_App.Application.DTOs.Auth;
using TVS_App.Application.Requests.AuthRequests;
using TVS_App.Domain.Shared;

namespace TVSApp.Web.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private const string AccessTokenKey = "jwtToken";
    private const string RefreshTokenKey = "refreshToken";

    private readonly HttpClient _httpClient;
    private readonly ISyncLocalStorageService _localStorage;
    private readonly TokenRefreshService _tokenRefreshService;

    public CustomAuthStateProvider(
        IHttpClientFactory httpClientFactory,
        ISyncLocalStorageService localStorageService,
        TokenRefreshService tokenRefreshService)
    {
        _httpClient = httpClientFactory.CreateClient("api");
        _localStorage = localStorageService;
        _tokenRefreshService = tokenRefreshService;

        var token = _localStorage.GetItem<string>(AccessTokenKey);
        if (!string.IsNullOrEmpty(token) && !IsTokenExpired(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _localStorage.GetItem<string>(AccessTokenKey);
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
        {
            token = await _tokenRefreshService.TryRefreshAsync();

            if (string.IsNullOrEmpty(token))
            {
                ClearSession();
                return new AuthenticationState(anonymous);
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public async Task<BaseResponse<string>> Login(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<BaseResponse<AuthTokensDto>>();
                var errorMessage = errorResponse?.Message ?? "Falha no login";
                return new BaseResponse<string>(errorMessage, (int)response.StatusCode, errorMessage);
            }

            var baseResponse = await response.Content.ReadFromJsonAsync<BaseResponse<AuthTokensDto>>();
            if (baseResponse is not { IsSuccess: true } || baseResponse.Data is null)
                return new BaseResponse<string>("Token inválido", baseResponse?.Code ?? 500, baseResponse?.Message ?? "Falha no login");

            SaveSession(baseResponse.Data.AccessToken, baseResponse.Data.RefreshToken);

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

    public async Task LogoutAsync()
    {
        var refreshToken = _localStorage.GetItem<string>(RefreshTokenKey);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                await _httpClient.PostAsJsonAsync("/logout", new RefreshTokenRequest(refreshToken));
            }
            catch
            {
                // best-effort — mesmo se a revogação no servidor falhar, a sessão local é encerrada
            }
        }

        ClearSession();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private void SaveSession(string accessToken, string refreshToken)
    {
        _localStorage.SetItem(AccessTokenKey, accessToken);
        _localStorage.SetItem(RefreshTokenKey, refreshToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private void ClearSession()
    {
        _localStorage.RemoveItem(AccessTokenKey);
        _localStorage.RemoveItem(RefreshTokenKey);
        _httpClient.DefaultRequestHeaders.Authorization = null;
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
