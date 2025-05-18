using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Commands.CustomerCommands;

public class GetCustomerByIdCommand : ICommand
{
    public long Id { get; set; }

    public void Validate()
    {
        if (Id == 0)
            throw new CommandException<GetCustomerByIdCommand>("O Id do cliente não pode ser 0");
    }
}