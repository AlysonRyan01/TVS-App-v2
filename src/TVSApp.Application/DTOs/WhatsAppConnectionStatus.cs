namespace TVS_App.Application.DTOs;

public enum WhatsAppConnectionState
{
    NotConfigured,
    Connected,
    Disconnected,
    Unavailable
}

public record WhatsAppConnectionStatus(WhatsAppConnectionState State, string? Reason = null)
{
    public string Label => State switch
    {
        WhatsAppConnectionState.Connected => "conectado",
        WhatsAppConnectionState.Disconnected => "desconectado",
        WhatsAppConnectionState.Unavailable => "indisponível",
        _ => "não configurado"
    };
}
