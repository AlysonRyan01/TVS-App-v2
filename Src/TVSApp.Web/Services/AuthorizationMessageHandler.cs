using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace TVSApp.Web.Services;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ISyncLocalStorageService _localStorage;
    private readonly NavigationManager _navigationManager;

    public AuthorizationMessageHandler(ISyncLocalStorageService localStorage, NavigationManager navigationManager)
    {
        _localStorage = localStorage;
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _localStorage.GetItem<string>("jwtToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response =  await base.SendAsync(request, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _localStorage.RemoveItem("jwtToken");
            _navigationManager.NavigateTo("/entrar");
        }

        return response;
    }
}