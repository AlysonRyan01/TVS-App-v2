using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace TVSApp.Web.Services;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ISyncLocalStorageService _localStorage;

    public AuthorizationMessageHandler(ISyncLocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _localStorage.GetItem<string>("jwtToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}