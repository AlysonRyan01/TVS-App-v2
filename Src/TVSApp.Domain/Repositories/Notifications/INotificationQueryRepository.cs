using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Domain.Repositories.Notifications;

public interface INotificationQueryRepository
{
    Task<BaseResponse<List<Notification>>> GetUnread();
}