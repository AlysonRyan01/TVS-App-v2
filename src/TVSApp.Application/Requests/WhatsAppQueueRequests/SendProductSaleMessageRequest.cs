using TVS_App.Domain.Enums;

namespace TVS_App.Application.Requests.WhatsAppQueueRequests;

public record SendProductSaleMessageRequest(
    string ServiceOrder,
    string CustomerName, 
    string PhoneNumber, 
    decimal Amount,
    string Guarantee, 
    EProduct ProductType)
    : IWhatsAppServiceRequest;