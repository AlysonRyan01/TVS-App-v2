using MediatR;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.NotificationRequests.NotificationQueries;

public record GetUnreadNotificationsQuery : IRequest<BaseResponse<List<Notification>>>;