using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Commands.ServiceOrderCommands;

public class GetServiceOrdersForCustomerCommand : ICommand
{
    public long ServiceOrderId { get; set; }
    public string SecurityCode { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrEmpty(SecurityCode))
            throw new CommandException<GetServiceOrdersForCustomerCommand>("O código de segurança não pode ser vazio");
    }

    public void Normalize()
    {
        SecurityCode = SecurityCode.Trim().ToUpper();
    }
}