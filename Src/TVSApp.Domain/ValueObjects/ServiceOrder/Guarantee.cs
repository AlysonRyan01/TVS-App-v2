using System.Text.Json.Serialization;
using TVS_App.Domain.Exceptions;

namespace TVS_App.Domain.ValueObjects.ServiceOrder;

public class Guarantee : ValueObject
{
    public Guarantee() {}

    [JsonConstructor]
    public Guarantee(string serviceOrderGuarantee)
    {
        ServiceOrderGuarantee = serviceOrderGuarantee;
    }

    public string ServiceOrderGuarantee { get; private set; } = string.Empty;
}