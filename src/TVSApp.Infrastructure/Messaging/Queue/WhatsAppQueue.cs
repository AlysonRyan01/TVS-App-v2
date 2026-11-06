using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Observability;
using TVS_App.Application.Requests.WhatsAppQueueRequests;

namespace TVS_App.Infrastructure.Messaging.Queue;

public class WhatsAppQueue : IWhatsAppQueue, IDisposable
{
    private readonly Channel<IWhatsAppServiceRequest> _queue;
    private readonly ILogger<WhatsAppQueue> _logger;

    public WhatsAppQueue(ILogger<WhatsAppQueue> logger)
    {
        _logger = logger;
        
        _queue = Channel.CreateBounded<IWhatsAppServiceRequest>(
            new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
    }

    public bool Enqueue(IWhatsAppServiceRequest job)
    {
        try
        {
            var messageType = job.GetType().Name;
            var serviceOrderId = ServiceOrderOf(job);

            using var scope = _logger.BeginScope(new Dictionary<string, object?> { ["Event"] = BusinessEvents.WhatsAppEnqueued });

            if (_queue.Writer.TryWrite(job))
            {
                _logger.LogInformation("WhatsApp enfileirado {MessageType:l} OS {ServiceOrderId:l}", messageType, serviceOrderId);
                return true;
            }

            _logger.LogWarning("WhatsApp recusado pela fila (canal fechado ou cheio) {MessageType:l} OS {ServiceOrderId:l}",
                messageType, serviceOrderId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro crítico ao enfileirar mensagem");
            throw;
        }
    }

    public static string? ServiceOrderOf(IWhatsAppServiceRequest job)
        => job.GetType().GetProperty("ServiceOrder")?.GetValue(job)?.ToString();

    public ChannelReader<IWhatsAppServiceRequest> Reader => _queue.Reader;

    public void Dispose()
    {
        _queue.Writer.Complete();
        _logger.LogInformation("Canal de WhatsApp fechado");
    }
}