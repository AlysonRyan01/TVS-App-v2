using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Observability;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Infrastructure.Messaging.Queue;
using TVS_App.Domain.Shared;

namespace TVS_App.Infrastructure.Messaging.BackgroundWorker;

public class WhatsAppNotificationWorker : BackgroundService
{
    private static readonly Meter Meter = new(BusinessEvents.MeterName);
    private static readonly Counter<long> Messages =
        Meter.CreateCounter<long>("whatsapp_messages_total", description: "Mensagens de WhatsApp por tipo e resultado");

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
                await ProcessAsync(request);
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

    private async Task ProcessAsync(IWhatsAppServiceRequest request)
    {
        var messageType = request.GetType().Name;
        var serviceOrderId = WhatsAppQueue.ServiceOrderOf(request);
        var phone = LogMask.Phone(PhoneOf(request));

        string outcome;
        try
        {
            var result = await SendAsync(request);
            if (result is null)
            {
                _logger.LogWarning("Tipo de requisição não suportado: {MessageType}", messageType);
                return;
            }

            outcome = result.IsSuccess ? "success" : "failure";
            Log(result.IsSuccess, messageType, serviceOrderId, phone, result.IsSuccess ? null : result.Message);
        }
        catch (Exception ex)
        {
            outcome = "failure";
            Log(false, messageType, serviceOrderId, phone, ex.Message, ex);
        }

        Messages.Add(1,
            new KeyValuePair<string, object?>("type", messageType),
            new KeyValuePair<string, object?>("outcome", outcome));
    }

    private void Log(bool success, string messageType, string? serviceOrderId, string phone, string? reason, Exception? ex = null)
    {
        var eventName = success ? BusinessEvents.WhatsAppSent : BusinessEvents.WhatsAppFailed;
        using var scope = _logger.BeginScope(new Dictionary<string, object?> { ["Event"] = eventName });

        if (success)
            _logger.LogInformation("WhatsApp enviado {MessageType:l} OS {ServiceOrderId:l} para {Phone:l}",
                messageType, serviceOrderId, phone);
        else
            _logger.LogWarning(ex, "WhatsApp falhou {MessageType:l} OS {ServiceOrderId:l} para {Phone:l}: {Reason}",
                messageType, serviceOrderId, phone, reason);
    }

    private static string? PhoneOf(IWhatsAppServiceRequest request)
        => request switch
        {
            SendSiteContactMessageRequest siteContact => siteContact.Phone,
            _ => request.GetType().GetProperty("PhoneNumber")?.GetValue(request) as string
        };

    private async Task<BaseResponse<string>?> SendAsync(IWhatsAppServiceRequest request)
    {
        switch (request)
        {
            case SendWelcomeMessageRequest welcome:
                return await _whatsappService.SendWelcomeMessage(
                    welcome.ServiceOrder,
                    welcome.CustomerName,
                    welcome.PhoneNumber);
            
            case SendEstimateMessageRequest estimate:
                return await _whatsappService.SendEstimateMessage(
                    estimate.ServiceOrder,
                    estimate.SecurityCode,
                    estimate.Result,
                    estimate.CustomerName,
                    estimate.Solution,
                    estimate.PhoneNumber);
            
            case SendDeviceReadyMessageRequest deviceReady:
                return await _whatsappService.SendDeviceReadyMessage(
                    deviceReady.ServiceOrder,
                    deviceReady.CustomerName,
                    deviceReady.PhoneNumber);
            
            case SendGuaranteeMessageRequest guarantee:
                return await _whatsappService.SendGuaranteeMessage(
                    guarantee.ServiceOrder,
                    guarantee.CustomerName,
                    guarantee.Guarantee,
                    guarantee.PhoneNumber);
            
            case SendProductSaleMessageRequest productSale:
                return await _whatsappService.SendProductSaleMessage(
                    productSale.CustomerName,
                    productSale.PhoneNumber,
                    productSale.Amount,
                    productSale.Guarantee,
                    productSale.ProductType);
            
            case SendRejectionReadyForPickupMessageRequest rejectionReady:
                return await _whatsappService.SendRejectionReadyForPickupMessage(
                    rejectionReady.CustomerName,
                    rejectionReady.PhoneNumber);
            
            case NotifyOwnerEstimateDecisionRequest notifyOwner:
                return await _whatsappService.NotifyOwnerEstimateDecision(
                    notifyOwner.ServiceOrder,
                    notifyOwner.Result,
                    notifyOwner.CustomerName);
            
            case SendThankYouForEstimateApprovalRequest thankYou:
                return await _whatsappService.SendThankYouForEstimateApprovalAsync(
                    thankYou.ServiceOrder,
                    thankYou.CustomerName,
                    thankYou.PhoneNumber,
                    thankYou.Amount);
            
            case SendEstimateDisapprovalAcknowledgmentRequest estimateDisapprovalAcknowledgment:
                return await _whatsappService.SendEstimateDisapprovalAcknowledgmentAsync(
                    estimateDisapprovalAcknowledgment.ServiceOrder,
                    estimateDisapprovalAcknowledgment.CustomerName,
                    estimateDisapprovalAcknowledgment.PhoneNumber);
            
            case SendSiteContactMessageRequest siteContact:
                return await _whatsappService.NotifyOwnerSiteContactAsync(siteContact);
            
            default:
                return null;
        }
    }
}
