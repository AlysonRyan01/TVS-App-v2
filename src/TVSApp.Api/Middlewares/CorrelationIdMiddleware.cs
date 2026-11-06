using System.Text.RegularExpressions;
using Serilog.Context;

namespace TVS_App.Api.Middlewares;

public partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var received = context.Request.Headers[HeaderName].ToString();
        var correlationId = ValidId().IsMatch(received) ? received : Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(ItemKey, correlationId))
        {
            await _next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex ValidId();
}
