using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TVS_App.Application.Interfaces.Services;
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
            if (_queue.Writer.TryWrite(job))
            {
                _logger.LogDebug("Mensagem enfileirada: {RequestType}", job.GetType().Name);
                return true;
            }

            _logger.LogWarning("Falha ao enfileirar mensagem (canal fechado ou cheio)");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro crítico ao enfileirar mensagem");
            throw;
        }
    }

    public ChannelReader<IWhatsAppServiceRequest> Reader => _queue.Reader;

    public void Dispose()
    {
        _queue.Writer.Complete();
        _logger.LogInformation("Canal de WhatsApp fechado");
    }
}