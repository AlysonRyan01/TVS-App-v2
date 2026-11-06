using TVS_App.Application.Exceptions;
using TVS_App.Domain.Enums;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record EditServiceOrderCommand(
    string? Brand,
    string Model,
    string SerialNumber,
    string? Defect,
    string? Accessories,
    EProduct Type,
    string? Location,
    EEnterprise Enterprise,
    DateTime? EntryDate,
    DateTime? InspectionDate,
    DateTime? ResponseDate,
    DateTime? RepairDate,
    DateTime? PurchasePartDate,
    DateTime? DeliveryDate,
    string? Solution,
    string? Guarantee,
    decimal PartCost,
    decimal LaborCost)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Model))
            throw new CommandException<EditServiceOrderCommand>("O modelo da ordem de serviço não pode estar vazio");

        if (string.IsNullOrWhiteSpace(SerialNumber))
            throw new CommandException<EditServiceOrderCommand>("O número de série da ordem de serviço não pode estar vazio");

        if (!Enum.IsDefined(Type))
            throw new CommandException<EditServiceOrderCommand>("Tipo de produto inválido.");

        if (!Enum.IsDefined(Enterprise))
            throw new CommandException<EditServiceOrderCommand>("Empresa inválida.");

        if (EntryDate is null)
            throw new CommandException<EditServiceOrderCommand>("A data de entrada é obrigatória.");

        if (PartCost < 0)
            throw new CommandException<EditServiceOrderCommand>("O custo da peça não pode ser negativo.");

        if (LaborCost < 0)
            throw new CommandException<EditServiceOrderCommand>("O custo de mão de obra não pode ser negativo.");
    }

    public EditServiceOrderCommand Normalize()
    {
        return this with
        {
            Brand = Clean(Brand),
            Model = Clean(Model),
            SerialNumber = Clean(SerialNumber),
            Defect = Clean(Defect),
            Accessories = Clean(Accessories),
            Location = Clean(Location),
            Solution = Clean(Solution),
            Guarantee = Clean(Guarantee)
        };
    }

    private static string Clean(string? value) => (value ?? string.Empty).Trim().ToUpper();
}
