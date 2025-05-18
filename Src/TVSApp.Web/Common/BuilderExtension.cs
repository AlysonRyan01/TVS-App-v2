using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using TVSApp.Web.Handlers;
using TVSApp.Web.Services;
using ServiceOrderHandler = TVSApp.Web.Handlers.ServiceOrderHandler;

namespace TVSApp.Web.Common;

public static class BuilderExtension
{
    public static void AddServices(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddTransient<CustomerHandler>();
        builder.Services.AddTransient<NotificationHandler>();
        builder.Services.AddTransient<AuthorizationMessageHandler>();
        builder.Services.AddTransient<ServiceOrderHandler>();
        builder.Services.AddScoped<CustomAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
        builder.Services.AddSingleton<HubConnection>(sp =>
        {
            return new HubConnectionBuilder()
                .WithUrl(new Uri("https://gerenciamentotvsapi-f7e9dyhyfbhrbpa8.brazilsouth-01.azurewebsites.net/osHub"), options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                })
                .WithAutomaticReconnect()
                .Build();
        });
    }
    
    public static void AddHttpClient(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddHttpClient("api", client =>
        {
            client.BaseAddress = new Uri("https://gerenciamentotvsapi-f7e9dyhyfbhrbpa8.brazilsouth-01.azurewebsites.net/");
            client.Timeout = TimeSpan.FromSeconds(60);
        }).AddHttpMessageHandler<AuthorizationMessageHandler>();
    }
}