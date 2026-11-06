using TVS_App.Application.DTOs;
using TVS_App.Application.Requests.WhatsAppQueueRequests;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Interfaces.Services;

public interface IWhatsappService
{
    Task<BaseResponse<string>> SendWelcomeMessage(string serviceOrder, string customerName, string phoneNumber);
    Task<BaseResponse<string>> SendEstimateMessage(string serviceOrder, string securityCode, ERepairResult? result, string customerName, string? solution, string phoneNumber);
    Task<BaseResponse<string>> SendDeviceReadyMessage(string serviceOrder, string customerName, string phoneNumber);
    Task<BaseResponse<string>> SendGuaranteeMessage(string serviceOrder, string customerName, string guarantee, string phoneNumber);
    Task<BaseResponse<string>> SendProductSaleMessage(string customerName, string phoneNumber, decimal amount, string guarantee, EProduct productType);
    Task<BaseResponse<string>> SendRejectionReadyForPickupMessage(string customerName, string phoneNumber);
    Task<BaseResponse<string>> NotifyOwnerEstimateDecision(string serviceOrder, ERepairStatus result, string customerName);
    Task<BaseResponse<string>> SendThankYouForEstimateApprovalAsync(string serviceOrder, string customerName, string phoneNumber, decimal amount);
    Task<BaseResponse<string>> SendEstimateDisapprovalAcknowledgmentAsync(string serviceOrder, string customerName, string phoneNumber);
    Task<BaseResponse<string>> NotifyOwnerSiteContactAsync(SendSiteContactMessageRequest request);

    /// <summary>Consulta se o número da loja está conectado na Evolution, sem enviar mensagem.</summary>
    Task<WhatsAppConnectionStatus> GetConnectionStateAsync(CancellationToken cancellationToken = default);

    /// <summary>Envia uma mensagem marcada como teste para o número informado (sob demanda).</summary>
    Task<BaseResponse<string>> SendTestMessageAsync(string? phoneNumber);
}