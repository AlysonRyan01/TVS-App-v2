using TVS_App.Application.Exceptions;
using TVS_App.Domain.Enums;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record AddServiceOrderEstimateCommand(
    long ServiceOrderId,
    string Warranty,
    string Solution,
    decimal TotalCost,
    ERepairResult RepairResult,
    string EstimateMessage,
    decimal PartPurchaseCost = 0
)
{
    public AddServiceOrderEstimateCommand() : this(
        0,
        string.Empty,
        string.Empty,
        0,
        default,
        string.Empty,
        0
    ) { }

    public AddServiceOrderEstimateCommand Normalize()
    {
        return this with
        {
            Solution = Solution.Trim().ToUpper(),
            Warranty = Warranty.Trim().ToUpper()
        };
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Solution))
            throw new CommandException<AddServiceOrderEstimateCommand>("A solução da ordem de serviço não pode estar vazia");

        if (string.IsNullOrWhiteSpace(EstimateMessage))
            throw new CommandException<AddServiceOrderEstimateCommand>("A mensagem de orçamento não pode estar vazia");

        if (TotalCost < 0)
            throw new CommandException<AddServiceOrderEstimateCommand>("O valor da ordem de serviço não pode ser menor que 0");

        if (PartPurchaseCost < 0)
            throw new CommandException<AddServiceOrderEstimateCommand>("O valor de compra da peça não pode ser menor que 0");

        if (!Enum.IsDefined(typeof(ERepairResult), RepairResult))
            throw new CommandException<AddServiceOrderEstimateCommand>("O resultado do orçamento da ordem de serviço é inválido");
    }
}