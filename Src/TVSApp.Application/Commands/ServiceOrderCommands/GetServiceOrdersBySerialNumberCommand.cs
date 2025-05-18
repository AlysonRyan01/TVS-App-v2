using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Commands.ServiceOrderCommands;

public class GetServiceOrdersBySerialNumberCommand : ICommand
{
    public string SerialNumber { get; set; } = string.Empty;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SerialNumber))
            throw new CommandException<GetServiceOrdersBySerialNumberCommand>("O número de série não pode ser vazio.");
    }
    
    public void Normalize()
    {
        SerialNumber = SerialNumber.Trim().ToUpper();
    }
}