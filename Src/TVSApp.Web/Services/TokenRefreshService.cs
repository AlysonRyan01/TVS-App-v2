using System.Net.Http.Json;
using Blazored.LocalStorage;
using TVS_App.Application.Requests.AuthRequests;
using TVS_App.Domain.Shared;

namespace TVSApp.Web.Services;

public class TokenRefreshService
{
    private readonly HttpClient _httpClient;
    private readonly ISyncLocalStorageService _localStorage;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public TokenRefreshService(IHttpClientFactory httpClientFactory, ISyncLocalStorageService localStorage)
    {
        _httpClient = httpClientFactory.CreateClient("api-raw");
        _localStorage = localStorage;
    }

    public async Task<string?> TryRefreshAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            var refreshToken = _localStorage.GetItem<string>("refreshToken");
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            var response = await _httpClient.PostAsJsonAsync("refresh-token", new RefreshTokenRequest(refreshToken));
            if (!response.IsSuccessStatusCode)
                return null;

            var baseResponse = await response.Content.ReadFromJsonAsync<BaseResponse<string>>();
            if (baseResponse is not { IsSuccess: true } || string.IsNullOrWhiteSpace(baseResponse.Data))
                return null;

            _localStorage.SetItem("jwtToken", baseResponse.Data);

            return baseResponse.Data;
        }
        catch
        {
            return null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
