namespace TVS_App.Application.Requests.WhatsAppQueueRequests;

public record SendThankYouForEstimateApprovalRequest(
    string ServiceOrder, 
    string CustomerName, 
    string PhoneNumber,
    decimal Amount)
    : IWhatsAppServiceRequest;