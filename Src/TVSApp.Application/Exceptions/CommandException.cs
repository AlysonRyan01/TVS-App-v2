using TVS_App.Application.Commands;

namespace TVS_App.Application.Exceptions;

public class CommandException <T> : Exception where T : ICommand
{
    public CommandException(string message) : base (message)
    {
        
    }
}