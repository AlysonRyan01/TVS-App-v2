using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Commands.ServiceOrderCommands;

public class AddProductLocationCommand : ICommand
{
    public long ServiceOrderId { get; set; }
    public string Location { get; set; } = string.Empty;

    public void Normalize()
    {
        Location = Location.Trim().ToUpper();
    }

    public void Validate()
    {
        if (ServiceOrderId == 0)
            throw new CommandException<AddProductLocationCommand>("O ID da ordem de serviço não pode ser 0");

        if (string.IsNullOrEmpty(Location))
            throw new CommandException<AddProductLocationCommand>("A localização não pode estar vazia");

    }
}