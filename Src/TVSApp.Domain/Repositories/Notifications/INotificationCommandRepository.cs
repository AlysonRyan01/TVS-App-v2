using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Domain.Repositories.Notifications;

public interface INotificationCommandRepository
{
    Task<BaseResponse<Notification>> Create(string title, string message);
    Task<BaseResponse<Notification>> MarkAsRead(long id);
}