using TVS_App.Domain.Exceptions;

namespace TVS_App.Domain.ValueObjects.ServiceOrder;

public class PartPurchaseCost : ValueObject
{
    protected PartPurchaseCost() { }

    public PartPurchaseCost(decimal serviceOrderPartPurchaseCost)
    {
        if (serviceOrderPartPurchaseCost < 0)
            throw new ValueObjectException<PartPurchaseCost>("O valor de compra da peça não pode ser menor que 0");

        ServiceOrderPartPurchaseCost = serviceOrderPartPurchaseCost;
    }

    public decimal ServiceOrderPartPurchaseCost { get; private set; }

    public override string ToString() => ServiceOrderPartPurchaseCost.ToString("C");
}
