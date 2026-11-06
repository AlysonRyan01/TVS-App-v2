using System.Diagnostics.Metrics;
using TVS_App.Application.DTOs;
using TVS_App.Application.Observability;

namespace TVS_App.Infrastructure.Services.Whatsapp;

/// <summary>Métricas de WhatsApp no meter "TVS" (exportadas como tvs_*).</summary>
public static class WhatsAppMetrics
{
    private static readonly Meter Meter = new(BusinessEvents.MeterName);

    private static readonly Counter<long> Messages =
        Meter.CreateCounter<long>("whatsapp_messages_total", description: "Mensagens de WhatsApp por tipo e resultado");

    private static WhatsAppConnectionStatus? _connection;

    static WhatsAppMetrics()
    {
        Meter.CreateObservableGauge("whatsapp_connected", ObserveConnection,
            description: "Conexão do WhatsApp da loja (1 conectado, 0 desconectado/indisponível)");
    }

    public static void RecordMessage(string type, bool success) =>
        Messages.Add(1,
            new KeyValuePair<string, object?>("type", type),
            new KeyValuePair<string, object?>("outcome", success ? "success" : "failure"));

    public static void SetConnection(WhatsAppConnectionStatus status) => _connection = status;

    private static IEnumerable<Measurement<int>> ObserveConnection()
    {
        var status = _connection;
        if (status is null || status.State == WhatsAppConnectionState.NotConfigured)
            return [];

        return [new Measurement<int>(status.State == WhatsAppConnectionState.Connected ? 1 : 0)];
    }
}
