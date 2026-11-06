using System.Diagnostics.Metrics;
using TVS_App.Application.Observability;

namespace TVS_App.Api.Observability;

public sealed class BusinessEventFilter : IEndpointFilter
{
    private static readonly Meter Meter = new(BusinessEvents.MeterName);
    private static readonly Counter<long> Events =
        Meter.CreateCounter<long>("business_events_total", description: "Eventos de negócio por resultado");

    private readonly string _eventName;

    public BusinessEventFilter(string eventName)
    {
        _eventName = eventName;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var result = await next(context);

        var statusCode = result is IStatusCodeHttpResult { StatusCode: { } code } ? code : http.Response.StatusCode;
        var success = statusCode is >= 200 and < 300;
        var outcome = success ? "success" : "failure";

        var logger = http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("BusinessEvents");
        var isCustomer = _eventName.StartsWith("customer.", StringComparison.Ordinal);
        var id = RouteId(http) ?? ArgumentId(context);
        var serviceOrderId = isCustomer ? null : id ?? ResponseServiceOrderId(http);
        var customerId = isCustomer ? id : null;

        logger.Log(
            success ? LogLevel.Information : LogLevel.Warning,
            "Evento {Event:l} OS {ServiceOrderId:l} cliente {CustomerId:l} → {Outcome:l} ({StatusCode})",
            _eventName, serviceOrderId, customerId, outcome, statusCode);

        Events.Add(1, new KeyValuePair<string, object?>("event", _eventName), new KeyValuePair<string, object?>("outcome", outcome));
        return result;
    }

    private static string? RouteId(HttpContext http)
        => http.Request.RouteValues.TryGetValue("id", out var value) ? value?.ToString() : null;

    private static string? ResponseServiceOrderId(HttpContext http)
    {
        var header = http.Response.Headers["X-Service-Order-Id"].ToString();
        return string.IsNullOrEmpty(header) ? null : header;
    }

    private static string? ArgumentId(EndpointFilterInvocationContext context)
    {
        foreach (var argument in context.Arguments)
        {
            var property = argument?.GetType().GetProperty("ServiceOrderId") ?? argument?.GetType().GetProperty("Id");
            if (property?.PropertyType == typeof(long))
                return property.GetValue(argument)?.ToString();
        }
        return null;
    }
}

public static class BusinessEventExtensions
{
    public static RouteHandlerBuilder WithBusinessEvent(this RouteHandlerBuilder builder, string eventName)
        => builder.AddEndpointFilter(new BusinessEventFilter(eventName));
}
