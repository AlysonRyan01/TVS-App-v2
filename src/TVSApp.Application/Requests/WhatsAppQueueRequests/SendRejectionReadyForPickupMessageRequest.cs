namespace TVS_App.Application.Requests.WhatsAppQueueRequests;

public record SendRejectionReadyForPickupMessageRequest(
    string CustomerName, 
    string PhoneNumber)
    : IWhatsAppServiceRequest;