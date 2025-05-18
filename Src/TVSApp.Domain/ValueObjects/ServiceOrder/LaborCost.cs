using System.Text.Json.Serialization;
using TVS_App.Domain.Exceptions;

namespace TVS_App.Domain.ValueObjects.ServiceOrder;

public class LaborCost : ValueObject
{
    protected LaborCost() { }

    [JsonConstructor]
    public LaborCost(decimal serviceOrderLaborCost)
    {
        if (serviceOrderLaborCost < 0)
            throw new ValueObjectException<LaborCost>("O valor da mão de obra não pode ser menor que 0");

        ServiceOrderLaborCost = serviceOrderLaborCost;
    }

    public decimal ServiceOrderLaborCost { get; private set; }

    public override string ToString() => ServiceOrderLaborCost.ToString("C");
}