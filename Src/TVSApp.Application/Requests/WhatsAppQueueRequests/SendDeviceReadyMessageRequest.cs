namespace TVS_App.Application.Requests.WhatsAppQueueRequests;

public record SendDeviceReadyMessageRequest(
    string ServiceOrder, 
    string CustomerName, 
    string PhoneNumber) 
    : IWhatsAppServiceRequest;