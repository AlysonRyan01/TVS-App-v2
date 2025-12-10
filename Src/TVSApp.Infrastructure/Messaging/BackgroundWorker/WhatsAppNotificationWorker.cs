using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Infrastructure.Messaging.Queue;

namespace TVS_App.Infrastructure.Messaging.BackgroundWorker;

public class WhatsAppNotificationWorker : BackgroundService
{
    private readonly IWhatsAppQueue _queue;
    private readonly IWhatsappService _whatsappService;
    private readonly ILogger<WhatsAppNotificationWorker> _logger;

    public WhatsAppNotificationWorker(
        IWhatsAppQueue queue,
        IWhatsappService whatsappService,
        ILogger<WhatsAppNotificationWorker> logger)
    {
        _queue = queue;
        _whatsappService = whatsappService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de notificações WhatsApp iniciado.");

        try
        {
            await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogDebug("Processando mensagem: {RequestType}", request.GetType().Name);

                    switch (request)
                    {
                        case SendWelcomeMessageRequest welcome:
                            await _whatsappService.SendWelcomeMessage(
                                welcome.ServiceOrder,
                                welcome.CustomerName,
                                welcome.PhoneNumber);
                            break;
                        
                        case SendEstimateMessageRequest estimate:
                            await _whatsappService.SendEstimateMessage(
                                estimate.ServiceOrder,
                                estimate.SecurityCode,
                                estimate.Result,
                                estimate.CustomerName,
                                estimate.Solution,
                                estimate.PhoneNumber);
                            break;
                        
                        case SendDeviceReadyMessageRequest deviceReady:
                            await _whatsappService.SendDeviceReadyMessage(
                                deviceReady.ServiceOrder,
                                deviceReady.CustomerName,
                                deviceReady.PhoneNumber);
                            break;
                        
                        case SendGuaranteeMessageRequest guarantee:
                            await _whatsappService.SendGuaranteeMessage(
                                guarantee.ServiceOrder,
                                guarantee.CustomerName,
                                guarantee.Guarantee,
                                guarantee.PhoneNumber);
                            break;
                        
                        case SendProductSaleMessageRequest productSale:
                            await _whatsappService.SendProductSaleMessage(
                                productSale.CustomerName,
                                productSale.PhoneNumber,
                                productSale.Amount,
                                productSale.Guarantee,
                                productSale.ProductType);
                            break;
                        
                        case SendRejectionReadyForPickupMessageRequest rejectionReady:
                            await _whatsappService.SendRejectionReadyForPickupMessage(
                                rejectionReady.CustomerName,
                                rejectionReady.PhoneNumber);
                            break;
                        
                        case NotifyOwnerEstimateDecisionRequest notifyOwner:
                            await _whatsappService.NotifyOwnerEstimateDecision(
                                notifyOwner.ServiceOrder,
                                notifyOwner.Result,
                                notifyOwner.CustomerName);
                            break;
                        
                        case SendThankYouForEstimateApprovalRequest thankYou:
                            await _whatsappService.SendThankYouForEstimateApprovalAsync(
                                thankYou.ServiceOrder,
                                thankYou.CustomerName,
                                thankYou.PhoneNumber,
                                thankYou.Amount);
                            break;
                        
                        case SendEstimateDisapprovalAcknowledgmentRequest estimateDisapprovalAcknowledgment:
                            await _whatsappService.SendEstimateDisapprovalAcknowledgmentAsync(
                                estimateDisapprovalAcknowledgment.ServiceOrder,
                                estimateDisapprovalAcknowledgment.CustomerName,
                                estimateDisapprovalAcknowledgment.PhoneNumber);
                            break;
                        
                        default:
                            _logger.LogWarning("Tipo de requisição não suportado: {RequestType}", request.GetType().Name);
                            break;
                    }

                    _logger.LogInformation("Mensagem processada com sucesso: {RequestType}", request.GetType().Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao processar mensagem do WhatsApp (Tipo: {RequestType})", request.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker parado devido a solicitação de cancelamento.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Falha crítica no worker de notificações WhatsApp");
            throw;
        }
        finally
        {
            _logger.LogInformation("Worker de notificações WhatsApp finalizado.");
        }
    }
}