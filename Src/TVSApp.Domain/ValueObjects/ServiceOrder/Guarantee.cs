using System.Text.Json.Serialization;
using TVS_App.Domain.Exceptions;

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