using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace TVS_App.IntegrationTests.Infrastructure;

public sealed record CapturedMessage(string Kind, string Instance, string Number, string Text, DateTime ReceivedAt);

/// <summary>
/// Substitui a Evolution API: registra cada mensagem que o sistema tentaria enviar e responde 200.
/// Nada sai da máquina.
/// </summary>
public sealed class FakeEvolutionHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<CapturedMessage> _messages = new();
    private readonly ConcurrentQueue<string> _otherRequests = new();

    public IReadOnlyCollection<CapturedMessage> Messages => _messages.ToArray();
    public IReadOnlyCollection<string> OtherRequests => _otherRequests.ToArray();

    /// <summary>Quando não nulo, todas as chamadas respondem com este status (simula Evolution com erro).</summary>
    public HttpStatusCode? ForcedStatus { get; set; }

    public void Clear()
    {
        _messages.Clear();
        _otherRequests.Clear();
        ForcedStatus = null;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

        if (path.Contains("/message/sendText/") || path.Contains("/message/sendMedia/"))
        {
            using var json = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var root = json.RootElement;
            string Get(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

            var isMedia = path.Contains("/message/sendMedia/");
            _messages.Enqueue(new CapturedMessage(
                isMedia ? "media" : "text",
                path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "",
                Get("number"),
                isMedia ? Get("caption") : Get("text"),
                DateTime.UtcNow));
        }
        else
        {
            _otherRequests.Enqueue($"{request.Method} {path}");
        }

        return new HttpResponseMessage(ForcedStatus ?? HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
    }

    public async Task<CapturedMessage?> WaitForAsync(Func<CapturedMessage, bool> match, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            var found = _messages.FirstOrDefault(match);
            if (found is not null)
                return found;
            await Task.Delay(100);
        }
        return _messages.FirstOrDefault(match);
    }

    public async Task<IReadOnlyList<CapturedMessage>> CollectAsync(Func<CapturedMessage, bool> match, TimeSpan? wait = null)
    {
        await Task.Delay(wait ?? TimeSpan.FromSeconds(2));
        return _messages.Where(match).ToList();
    }

    // O HttpClientFactory descarta handlers periodicamente; este é compartilhado pela execução inteira.
    protected override void Dispose(bool disposing) { }
}
