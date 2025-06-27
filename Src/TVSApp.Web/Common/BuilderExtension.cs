using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using MudBlazor.Services;
using TVSApp.Web.Handlers;
using TVSApp.Web.Services;
using ServiceOrderHandler = TVSApp.Web.Handlers.ServiceOrderHandler;

namespace TVSApp.Web.Common;

public static class BuilderExtension
{
    public static void AddServices(this WebAssemblyHostBuilder builder)
    {
        var baseAddress = builder.HostEnvironment.IsDevelopment()
            ? "http://localhost:5119/"
            : "https://gerenciamentotvsapi-f7e9dyhyfbhrbpa8.brazilsouth-01.azurewebsites.net/";
        
        builder.Services.AddTransient<CustomerHandler>();
        builder.Services.AddTransient<NotificationHandler>();
        builder.Services.AddTransient<AuthorizationMessageHandler>();
        builder.Services.AddTransient<ServiceOrderHandler>();
        builder.Services.AddScoped<CustomAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
        builder.Services.AddSingleton<HubConnection>(sp =>
        {
            return new HubConnectionBuilder()
                .WithUrl(new Uri($"{baseAddress}osHub"), options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                })
                .WithAutomaticReconnect()
                .Build();
        });
        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
            config.SnackbarConfiguration.HideTransitionDuration = 100;
            config.SnackbarConfiguration.ShowTransitionDuration = 100;
            config.SnackbarConfiguration.VisibleStateDuration = 1000;
            config.SnackbarConfiguration.ShowCloseIcon = true;
        });
    }
    
    public static void AddHttpClient(this WebAssemblyHostBuilder builder)
    {
        var baseAddress = builder.HostEnvironment.IsDevelopment()
            ? "http://localhost:5119/"
            : "https://gerenciamentotvsapi-f7e9dyhyfbhrbpa8.brazilsouth-01.azurewebsites.net/";
        
        builder.Services.AddHttpClient("api", client =>
        {
            client.BaseAddress = new Uri(baseAddress);
            client.Timeout = TimeSpan.FromSeconds(60);
        }).AddHttpMessageHandler<AuthorizationMessageHandler>();
    }
}