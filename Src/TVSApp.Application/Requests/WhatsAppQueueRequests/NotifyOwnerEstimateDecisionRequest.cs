using TVS_App.Domain.Enums;

namespace TVS_App.Application.Requests.WhatsAppQueueRequests;

public record NotifyOwnerEstimateDecisionRequest(
    string ServiceOrder, 
    ERepairStatus Result, 
    string CustomerName)
    : IWhatsAppServiceRequest;