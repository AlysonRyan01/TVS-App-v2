using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TVSApp.Web;
using TVSApp.Web.Common;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();
builder.AddServices();
builder.AddHttpClient();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddMudServices();

await builder.Build().RunAsync();
