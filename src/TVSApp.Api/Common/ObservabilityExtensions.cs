using System.Security.Claims;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using TVS_App.Api.Middlewares;
using TVS_App.Application.Observability;

namespace TVS_App.Api.Common;

public static class ObservabilityExtensions
{
    private const string PrometheusPath = "/internal/metrics";
    private static readonly HashSet<string> IgnoredPaths = ["/", PrometheusPath];

    public static void AddObservability(this WebApplicationBuilder builder)
    {
        var isDevelopment = builder.Environment.IsDevelopment();

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service", "tvs-api")
                .Enrich.WithProperty("environment", context.HostingEnvironment.EnvironmentName);

            if (isDevelopment)
                configuration.WriteTo.Console();
            else
                configuration.WriteTo.Console(new RenderedCompactJsonFormatter());
        });

        // Só os contadores do meter "TVS" (exportados como tvs_<instrumento>); sem os EventCounters do .NET.
        // O adaptador é ligado já na subida para não perder eventos anteriores à primeira coleta.
        Metrics.SuppressDefaultMetrics(new SuppressDefaultMetricOptions { SuppressMeters = true, SuppressEventCounters = true });
        MeterAdapter.StartListening(new MeterAdapterOptions
        {
            InstrumentFilterPredicate = instrument => instrument.Meter.Name == BusinessEvents.MeterName
        });
    }

    public static void UseObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod:l} {RequestPath:l} → {StatusCode} em {Elapsed:0} ms";
            options.GetLevel = (context, _, exception) =>
                IgnoredPaths.Contains(context.Request.Path.Value ?? string.Empty) ? LogEventLevel.Verbose
                : exception is not null || context.Response.StatusCode >= 500 ? LogEventLevel.Error
                : LogEventLevel.Information;
            options.GetMessageTemplateProperties = (context, requestPath, elapsed, statusCode) =>
            [
                new LogEventProperty("RequestMethod", new ScalarValue(context.Request.Method)),
                new LogEventProperty("RequestPath", new ScalarValue(RouteTemplate(context) ?? requestPath)),
                new LogEventProperty("StatusCode", new ScalarValue(statusCode)),
                new LogEventProperty("Elapsed", new ScalarValue(elapsed))
            ];
            options.EnrichDiagnosticContext = (diagnostic, context) =>
            {
                var serviceOrderId = ServiceOrderIdOf(context);
                if (serviceOrderId is not null)
                    diagnostic.Set("ServiceOrderId", serviceOrderId);

                var email = context.User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(email))
                    diagnostic.Set("UserEmail", email);
            };
        });

        app.UseRouting();
        app.UseHttpMetrics();
    }

    private static string? ServiceOrderIdOf(HttpContext context)
    {
        var header = context.Response.Headers["X-Service-Order-Id"].ToString();
        if (!string.IsNullOrEmpty(header))
            return header;

        var isServiceOrderRoute = context.Request.Path.StartsWithSegments("/service-orders");
        return isServiceOrderRoute && context.Request.RouteValues.TryGetValue("id", out var id) ? id?.ToString() : null;
    }

    private static string? RouteTemplate(HttpContext context)
    {
        var template = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
        if (template is null)
            return null;

        return template.StartsWith('/') ? template : "/" + template;
    }

    public static void MapObservability(this WebApplication app)
    {
        app.MapMetrics(PrometheusPath);
    }
}
