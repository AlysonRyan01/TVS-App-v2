namespace TVS_App.Infrastructure.Configurations;

public class WhatsAppMonitorSettings
{
    /// <summary>Intervalo entre as consultas do estado da conexão, em segundos.</summary>
    public int IntervalSeconds { get; set; } = 60;
}
