using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Commands.NotificationCommands;

public record CreateNotificationCommand(string Title, string Message) : ICommand
{
    public void Validate()
    {
        if (string.IsNullOrEmpty(Title))
            throw new CommandException<CreateNotificationCommand>("Título vazio");
        
        if (string.IsNullOrEmpty(Message))
            throw new CommandException<CreateNotificationCommand>("Mensagem vazia");
    }
}