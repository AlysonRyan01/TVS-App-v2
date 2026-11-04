using MediatR;
using TVS_App.Application.Exceptions;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.NotificationRequests.NotificationCommands;

public record CreateNotificationCommand(string Title, string Message) : IRequest<BaseResponse<Notification>>
{
    public void Validate()
    {
        if (string.IsNullOrEmpty(Title))
            throw new CommandException<CreateNotificationCommand>("Título vazio");
        
        if (string.IsNullOrEmpty(Message))
            throw new CommandException<CreateNotificationCommand>("Mensagem vazia");
    }
}