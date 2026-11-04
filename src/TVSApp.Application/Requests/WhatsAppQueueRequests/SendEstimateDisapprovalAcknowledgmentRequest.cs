namespace TVS_App.Application.Requests.WhatsAppQueueRequests;

public record SendEstimateDisapprovalAcknowledgmentRequest(
    string ServiceOrder, 
    string CustomerName, 
    string PhoneNumber)
    : IWhatsAppServiceRequest;