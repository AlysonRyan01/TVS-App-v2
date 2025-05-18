using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Commands.ServiceOrderCommands;

public class GetServiceOrdersByModelCommand : ICommand
{
    public string Model { get; set; } = string.Empty;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Model))
            throw new CommandException<GetServiceOrdersByModelCommand>("O modelo não pode ser vazio.");
    }
    
    public void Normalize()
    {
        Model = Model.Trim().ToUpper();
    }
}