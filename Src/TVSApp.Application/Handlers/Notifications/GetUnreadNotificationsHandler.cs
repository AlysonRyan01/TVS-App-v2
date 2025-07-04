using TVS_App.Application.Queries;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Notifications;

public class GetUnreadNotificationsHandler : IQueryHandler<EmptyQuery, BaseResponse<List<Notification>>>
{
    private readonly INotificationQueryRepository _notificationQueryRepository;

    public GetUnreadNotificationsHandler(INotificationQueryRepository notificationQueryRepository)
    {
        _notificationQueryRepository = notificationQueryRepository;
    }

    public async Task<BaseResponse<List<Notification>>> HandleAsync(EmptyQuery query)
    {
        return await _notificationQueryRepository.GetUnread();
    }
}