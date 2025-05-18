using System.Text.Json.Serialization;
using TVS_App.Domain.Exceptions;

namespace TVS_App.Domain.ValueObjects.Customer;

public class Phone : ValueObject
{
    public Phone() { }

    [JsonConstructor]
    public Phone(string customerPhone)
    {
        CustomerPhone = customerPhone;
    }
    
    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; private set; } = string.Empty;

    public override string ToString() => CustomerPhone;
}