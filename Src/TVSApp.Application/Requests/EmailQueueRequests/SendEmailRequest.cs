using Microsoft.AspNetCore.Http;

namespace TVS_App.Application.Requests.EmailQueueRequests;

public record SendEmailRequest(
    string Name,
    string Email,
    string Phone,
    string Message,
    List<IFormFile>? Files = null) : IEmailServiceRequest;