using TVS_App.Application.Commands;

namespace TVS_App.Application.Exceptions;

public class CommandException <T> : CommandException where T : ICommand
{
    public CommandException(string message) : base (message) { }
}

public abstract class CommandException : Exception
{
    protected CommandException(string message) : base (message) { }
}