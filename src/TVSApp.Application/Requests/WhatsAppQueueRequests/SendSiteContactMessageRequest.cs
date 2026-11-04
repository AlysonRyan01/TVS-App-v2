namespace TVS_App.Application.Requests.WhatsAppQueueRequests;

public record WhatsAppMediaAttachment(
    string FileName,
    string MimeType,
    string Base64);

public record SendSiteContactMessageRequest(
    string Subject,
    string Name,
    string Phone,
    string? Email,
    string? Message,
    List<WhatsAppMediaAttachment> Attachments)
    : IWhatsAppServiceRequest;
