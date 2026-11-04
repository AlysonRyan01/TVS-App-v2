using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.EmailQueueRequests;

namespace TVS_App.Infrastructure.Messaging.BackgroundWorker;

public class EmailNotificationWorker : BackgroundService
{
    private readonly IEmailQueue _queue;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailNotificationWorker> _logger;

    public EmailNotificationWorker(
        IEmailQueue queue,
        IEmailService emailService,
        ILogger<EmailNotificationWorker> logger)
    {
        _queue = queue;
        _emailService = emailService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de notificações email iniciado.");

        try
        {
            await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogDebug("Processando email: {RequestType}", request.GetType().Name);

                    switch (request)
                    {
                        case SendEmailRequest send:
                            await _emailService.Send(send);
                            break;
                        
                        default:
                            _logger.LogWarning("Tipo de requisição não suportado: {RequestType}",
                                request.GetType().Name);
                            break;
                    }

                    _logger.LogInformation("Mensagem processada com sucesso: {RequestType}", request.GetType().Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao processar mensagem de email (Tipo: {RequestType})",
                        request.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker parado devido a solicitação de cancelamento.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Falha crítica no worker de notificações email");
            throw;
        }
        finally
        {
            _logger.LogInformation("Worker de notificações email finalizado.");
        }
    }
}