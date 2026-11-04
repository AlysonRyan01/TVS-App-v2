namespace TVS_App.Application.Requests.WhatsAppQueueRequests;

public record SendGuaranteeMessageRequest(
    string ServiceOrder, 
    string CustomerName, 
    string Guarantee, 
    string PhoneNumber) 
    : IWhatsAppServiceRequest;