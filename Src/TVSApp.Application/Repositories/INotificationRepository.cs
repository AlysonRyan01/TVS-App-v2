using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Repositories;

public interface INotificationRepository
{
    Task<BaseResponse<Notification>> Create(string title, string message);
    Task<BaseResponse<List<Notification>>> GetUnread();
    Task<BaseResponse<Notification>> MarkAsRead(long id);
}