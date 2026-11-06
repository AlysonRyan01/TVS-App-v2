using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record AddPaymentCommand(decimal Amount, DateTime PaymentDate)
{
    public void Validate()
    {
        if (Amount <= 0)
            throw new CommandException<AddPaymentCommand>("O valor deve ser maior que zero");

        if (PaymentDate == default)
            throw new CommandException<AddPaymentCommand>("Informe a data do pagamento.");
    }
}
