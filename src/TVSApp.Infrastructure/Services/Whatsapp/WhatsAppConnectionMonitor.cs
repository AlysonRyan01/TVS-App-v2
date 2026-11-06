using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TVS_App.Application.DTOs;
using TVS_App.Application.Interfaces.Services;

namespace TVS_App.Infrastructure.Services.Whatsapp;

/// <summary>
/// Consulta periodicamente se o WhatsApp da loja está conectado na Evolution — sem enviar mensagens —
/// e publica o estado em tvs_whatsapp_connected. Só registra log quando o estado muda.
/// </summary>
public sealed class WhatsAppConnectionMonitor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WhatsAppConnectionMonitor> _logger;
    private readonly TimeSpan _interval;
    private WhatsAppConnectionState? _lastState;

    public WhatsAppConnectionMonitor(
        IServiceScopeFactory scopeFactory,
        IOptions<Configurations.WhatsAppMonitorSettings> settings,
        ILogger<WhatsAppConnectionMonitor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(Math.Max(10, settings.Value.IntervalSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        do
        {
            await CheckAsync(stoppingToken);
        }
        while (await WaitNextAsync(timer, stoppingToken));
    }

    public async Task CheckAsync(CancellationToken cancellationToken)
    {
        WhatsAppConnectionStatus status;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var whatsapp = scope.ServiceProvider.GetRequiredService<IWhatsappService>();
            status = await whatsapp.GetConnectionStateAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            status = new WhatsAppConnectionStatus(WhatsAppConnectionState.Unavailable, ex.Message);
        }

        WhatsAppMetrics.SetConnection(status);
        LogIfChanged(status);
    }

    private void LogIfChanged(WhatsAppConnectionStatus status)
    {
        if (_lastState == status.State)
            return;

        var first = _lastState is null;
        _lastState = status.State;

        if (status.State == WhatsAppConnectionState.NotConfigured)
        {
            if (first)
                _logger.LogInformation("WhatsApp conexão: monitor desligado (Evolution não configurada)");
            return;
        }

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["Event"] = "whatsapp.connection",
            ["State"] = status.Label
        });

        if (status.State == WhatsAppConnectionState.Connected)
            _logger.LogInformation("WhatsApp conexão: conectado");
        else
            _logger.LogWarning("WhatsApp conexão: {State:l} ({Reason:l})", status.Label, status.Reason ?? "sem detalhes");
    }

    private static async Task<bool> WaitNextAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try { return await timer.WaitForNextTickAsync(stoppingToken); }
        catch (OperationCanceledException) { return false; }
    }
}
