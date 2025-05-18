using System.Text.Json.Serialization;
using TVS_App.Domain.Exceptions;

namespace TVS_App.Domain.ValueObjects.Customer;

public class Email : ValueObject
{
    public Email() { }

    [JsonConstructor]
    public Email(string customerEmail)
    {
        CustomerEmail = customerEmail;
    }
    
    [JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; private set; } = string.Empty;

    public override string ToString() => CustomerEmail;
}