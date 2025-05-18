using System.Text.Json.Serialization;
using TVS_App.Domain.Exceptions;

namespace TVS_App.Domain.ValueObjects.Customer;

public class Name : ValueObject
{
    public Name() {}

    [JsonConstructor]
    public Name(string customerName)
    {
        if (string.IsNullOrEmpty(customerName))
            throw new ValueObjectException<Name>("O nome do cliente não pode estar vazio");

        CustomerName = customerName;
    }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; private set; } = string.Empty;

    public override string ToString() => CustomerName;
}