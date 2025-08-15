using TVS_App.Application.Requests.EmailQueueRequests;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Interfaces.Services;

public interface IEmailService
{
    Task<BaseResponse<string>> Send(SendEmailRequest request, CancellationToken cancellationToken = default);
}