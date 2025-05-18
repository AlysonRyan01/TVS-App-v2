using TVS_App.Application.DTOs;
using TVS_App.Application.Repositories;
using TVS_App.Domain.Entities;

namespace TVS_App.Application.Handlers;

public class NotificationHandler
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }
    
    public async Task<BaseResponse<Notification>> CreateNotification(string title, string message)
    {
        return await _notificationRepository.Create(title, message);
    }

    public async Task<BaseResponse<List<Notification>>> GetUnreadNotifications()
    {
        return await _notificationRepository.GetUnread();
    }

    public async Task<BaseResponse<Notification>> MarkNotificationAsRead(long id)
    {
        return await _notificationRepository.MarkAsRead(id);
    }
}