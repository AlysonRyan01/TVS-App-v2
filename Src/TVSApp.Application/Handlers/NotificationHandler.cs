using TVS_App.Application.Interfaces.Handlers;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers;

public class NotificationHandler : INotificationHandler
{
    private readonly INotificationCommandRepository _notificationCommandRepository;
    private readonly INotificationQueryRepository _notificationQueryRepository;

    public NotificationHandler(
        INotificationCommandRepository notificationCommandRepository,
        INotificationQueryRepository notificationQueryRepository)
    {
        _notificationCommandRepository = notificationCommandRepository;
        _notificationQueryRepository = notificationQueryRepository;
    }
    
    public async Task<BaseResponse<Notification>> CreateNotification(string title, string message)
    {
        return await _notificationCommandRepository.Create(title, message);
    }

    public async Task<BaseResponse<List<Notification>>> GetUnreadNotifications()
    {
        return await _notificationQueryRepository.GetUnread();
    }

    public async Task<BaseResponse<Notification>> MarkNotificationAsRead(long id)
    {
        return await _notificationCommandRepository.MarkAsRead(id);
    }
}