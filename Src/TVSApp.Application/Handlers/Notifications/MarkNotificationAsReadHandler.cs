using MediatR;
using TVS_App.Application.Requests.NotificationRequests.NotificationCommands;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Notifications;

public class MarkNotificationAsReadHandler : IRequestHandler<MarkNotificationAsReadCommand, BaseResponse<Notification>>
{
    private readonly INotificationCommandRepository _notificationCommandRepository;

    public MarkNotificationAsReadHandler(INotificationCommandRepository notificationCommandRepository)
    {
        _notificationCommandRepository = notificationCommandRepository;
    }

    public async Task<BaseResponse<Notification>> Handle(
        MarkNotificationAsReadCommand request, 
        CancellationToken cancellationToken = default)
    {
        return await _notificationCommandRepository.MarkAsRead(request.Id);
    }
}