using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using TVS_App.Infrastructure.Configurations;
using TVS_App.Infrastructure.Services.Whatsapp;

namespace TVSApp.Tests.Infrastructure;

internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _respond;
    public List<(HttpRequestMessage Request, string Body)> Calls { get; } = [];

    public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) => _respond = respond;

    public static StubHandler Json(HttpStatusCode status, string json) =>
        new(_ => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") }));

    public static StubHandler Throws(Exception ex) => new(_ => Task.FromException<HttpResponseMessage>(ex));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
        Calls.Add((request, body));
        return await _respond(request);
    }
}

internal sealed class FakeEnv(string name) : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = name;
    public string ApplicationName { get; set; } = "TVSApp";
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

internal static class WhatsAppServiceTestKit
{
    public static WhatsappService Create(StubHandler handler, string baseUrl = "http://evolution.test", string environment = "Production") =>
        new(new HttpClient(handler),
            Options.Create(new EvolutionApiSettings { BaseUrl = baseUrl, Instance = "tvs", ApiKey = "chave" }),
            new FakeEnv(environment));
}
