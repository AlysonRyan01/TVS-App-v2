using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Interfaces.Handlers;

public interface INotificationHandler
{
    Task<BaseResponse<Notification>> CreateNotification(string title, string message);
    Task<BaseResponse<List<Notification>>> GetUnreadNotifications();
    Task<BaseResponse<Notification>> MarkNotificationAsRead(long id);
}