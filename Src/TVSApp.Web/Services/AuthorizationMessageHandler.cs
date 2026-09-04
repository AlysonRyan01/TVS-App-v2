using System.Net;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace TVSApp.Web.Services;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ISyncLocalStorageService _localStorage;
    private readonly NavigationManager _navigationManager;
    private readonly TokenRefreshService _tokenRefreshService;

    public AuthorizationMessageHandler(
        ISyncLocalStorageService localStorage,
        NavigationManager navigationManager,
        TokenRefreshService tokenRefreshService)
    {
        _localStorage = localStorage;
        _navigationManager = navigationManager;
        _tokenRefreshService = tokenRefreshService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _localStorage.GetItem<string>("jwtToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var newAccessToken = await _tokenRefreshService.TryRefreshAsync();

            if (!string.IsNullOrWhiteSpace(newAccessToken))
            {
                var retryRequest = await CloneRequestAsync(request);
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

                response.Dispose();
                response = await base.SendAsync(retryRequest, cancellationToken);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _localStorage.RemoveItem("jwtToken");
                _localStorage.RemoveItem("refreshToken");
                _navigationManager.NavigateTo("/entrar");
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}
