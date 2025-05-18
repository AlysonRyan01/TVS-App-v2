using System.Text.Json.Serialization;
using TVS_App.Domain.Exceptions;

namespace TVS_App.Domain.ValueObjects.Customer;

public class Address : ValueObject
{
    public Address() { }

    [JsonConstructor]
    public Address(string street,
        string neighborhood,
        string city,
        string number,
        string zipCode,
        string state)
    {
        if (state.Length > 2)
            throw new ValueObjectException<Address>("O estado não pode ser maior que 2 caracteres");

        Street = street;
        Neighborhood = neighborhood;
        City = city;
        Number = number;
        ZipCode = zipCode;
        State = state;
    }

    [JsonPropertyName("street")]
    public string Street { get; private set; } = string.Empty;
    [JsonPropertyName("neighborhood")]
    public string Neighborhood { get; private set; } = string.Empty;
    [JsonPropertyName("city")]
    public string City { get; private set; } = string.Empty;
    [JsonPropertyName("number")]
    public string Number { get; private set; } = string.Empty;
    [JsonPropertyName("zipCode")]
    public string ZipCode { get; private set; } = string.Empty;
    [JsonPropertyName("state")]
    public string State { get; private set; } = string.Empty;
    
    public override string ToString()
    {
        return $"{Street}, {Number}, {Neighborhood}, {City}, {State}, {ZipCode}";
    }
}