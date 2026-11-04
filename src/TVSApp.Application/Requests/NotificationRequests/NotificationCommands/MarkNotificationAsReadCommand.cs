using MediatR;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.NotificationRequests.NotificationCommands;

public record MarkNotificationAsReadCommand(long Id) : IRequest<BaseResponse<Notification>>;