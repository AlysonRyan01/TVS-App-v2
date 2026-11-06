using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record UpdatePaymentCommand(decimal Amount, DateTime PaymentDate)
{
    public void Validate()
    {
        if (Amount <= 0)
            throw new CommandException<UpdatePaymentCommand>("O valor deve ser maior que zero");

        if (PaymentDate == default)
            throw new CommandException<UpdatePaymentCommand>("Informe a data do pagamento.");
    }
}
