using MediatR;
using TVS_App.Application.Requests.NotificationRequests.NotificationQueries;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Notifications;

public class GetUnreadNotificationsHandler : IRequestHandler<GetUnreadNotificationsQuery, BaseResponse<List<Notification>>>
{
    private readonly INotificationQueryRepository _notificationQueryRepository;

    public GetUnreadNotificationsHandler(INotificationQueryRepository notificationQueryRepository)
    {
        _notificationQueryRepository = notificationQueryRepository;
    }

    public async Task<BaseResponse<List<Notification>>> Handle(
        GetUnreadNotificationsQuery request, 
        CancellationToken cancellationToken = default)
    {
        return await _notificationQueryRepository.GetUnread();
    }
}