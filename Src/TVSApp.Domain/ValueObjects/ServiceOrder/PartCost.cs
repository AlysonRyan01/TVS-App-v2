using System.Text.Json.Serialization;
using TVS_App.Domain.Exceptions;

namespace TVS_App.Domain.ValueObjects.ServiceOrder;

public class PartCost : ValueObject
{
    protected PartCost() { }

    [JsonConstructor]
    public PartCost(decimal serviceOrderPartCost)
    {
        if (serviceOrderPartCost < 0)
            throw new ValueObjectException<PartCost>("O valor da peça não pode ser menor que 0");

        ServiceOrderPartCost = serviceOrderPartCost;
    }

    public decimal ServiceOrderPartCost { get; private set; }

    public override string ToString() => ServiceOrderPartCost.ToString("C");
}