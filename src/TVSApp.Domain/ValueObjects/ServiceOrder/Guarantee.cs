namespace TVS_App.Domain.ValueObjects.ServiceOrder;

public class Guarantee : ValueObject
{
    protected Guarantee() {}
    
    public Guarantee(string serviceOrderGuarantee)
    {
        ServiceOrderGuarantee = serviceOrderGuarantee;
    }

    public string ServiceOrderGuarantee { get; private set; } = string.Empty;
}