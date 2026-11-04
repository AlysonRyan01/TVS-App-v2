using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.EmailQueueRequests;

namespace TVS_App.Infrastructure.Messaging.Queue;

public class EmailQueue : IEmailQueue, IDisposable
{
    private readonly Channel<IEmailServiceRequest> _queue;
    private readonly ILogger<EmailQueue> _logger;

    public EmailQueue(ILogger<EmailQueue> logger)
    {
        _logger = logger;
        
        _queue = Channel.CreateBounded<IEmailServiceRequest>(
            new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
    }

    public bool Enqueue(IEmailServiceRequest job)
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

    public ChannelReader<IEmailServiceRequest> Reader => _queue.Reader;

    public void Dispose()
    {
        _queue.Writer.Complete();
        _logger.LogInformation("Canal de email fechado");
    }
}