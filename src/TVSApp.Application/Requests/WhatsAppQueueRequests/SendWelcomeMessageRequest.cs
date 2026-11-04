namespace TVS_App.Application.Requests.WhatsAppQueueRequests;

public record SendWelcomeMessageRequest(
    string ServiceOrder, 
    string CustomerName, 
    string PhoneNumber) 
    : IWhatsAppServiceRequest;