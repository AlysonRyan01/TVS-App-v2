using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record AddProductLocationCommand(
    long ServiceOrderId,
    string Location
)
{
    public AddProductLocationCommand() : this(0, string.Empty) { }

    public AddProductLocationCommand Normalize()
    {
        return this with
        {
            Location = Location.Trim().ToUpper()
        };
    }

    public void Validate()
    {
        if (ServiceOrderId == 0)
            throw new CommandException<AddProductLocationCommand>("O ID da ordem de serviço não pode ser 0");

        if (string.IsNullOrWhiteSpace(Location))
            throw new CommandException<AddProductLocationCommand>("A localização não pode estar vazia");
    }
}