using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Commands.ServiceOrderCommands;

public class GetServiceOrderByIdCommand : ICommand
{
    public long Id { get; set; }

    public void Validate()
    {
        if (Id == 0)
            throw new CommandException<GetServiceOrderByIdCommand>("O Id da ordem de serviço não pode ser 0");
    }
    
    
}
