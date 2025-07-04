using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Notifications;

public class MarkNotificationAsReadHandler : ICommandHandler<long, BaseResponse<Notification>>
{
    private readonly INotificationCommandRepository _notificationCommandRepository;

    public MarkNotificationAsReadHandler(INotificationCommandRepository notificationCommandRepository)
    {
        _notificationCommandRepository = notificationCommandRepository;
    }

    public async Task<BaseResponse<Notification>> HandleAsync(long id)
    {
        return await _notificationCommandRepository.MarkAsRead(id);
    }
}