using TVS_App.Domain.Enums;

namespace TVS_App.Application.Requests.WhatsAppQueueRequests;

public record SendEstimateMessageRequest(
    string ServiceOrder, 
    string SecurityCode, 
    ERepairResult? Result, 
    string CustomerName, 
    string? Solution, 
    string PhoneNumber)
    : IWhatsAppServiceRequest;