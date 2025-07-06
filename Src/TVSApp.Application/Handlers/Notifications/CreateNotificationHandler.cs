using MediatR;
using TVS_App.Application.Requests.NotificationRequests.NotificationCommands;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Notifications;

public class CreateNotificationHandler : IRequestHandler<CreateNotificationCommand, BaseResponse<Notification>>
{
    private readonly INotificationCommandRepository _notificationCommandRepository;

    public CreateNotificationHandler(INotificationCommandRepository notificationCommandRepository)
    {
        _notificationCommandRepository = notificationCommandRepository;
    }
    
    public async Task<BaseResponse<Notification>> Handle(
        CreateNotificationCommand request, 
        CancellationToken cancellationToken = default)
    {
        return await _notificationCommandRepository.Create(request.Title, request.Message);
    }
}