using TVS_App.Application.Commands.NotificationCommands;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Handlers.Notifications;

public class CreateNotificationHandler : ICommandHandler<CreateNotificationCommand, BaseResponse<Notification>>
{
    private readonly INotificationCommandRepository _notificationCommandRepository;

    public CreateNotificationHandler(INotificationCommandRepository notificationCommandRepository)
    {
        _notificationCommandRepository = notificationCommandRepository;
    }

    public async Task<BaseResponse<Notification>> HandleAsync(CreateNotificationCommand command)
    {
        return await _notificationCommandRepository.Create(command.Title, command.Message);
    }
}