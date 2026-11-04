using TVS_App.Domain.Exceptions;

namespace TVS_App.Domain.ValueObjects.Customer;

public class Address : ValueObject
{
    protected Address() { }
    
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
    
    public string Street { get; private set; } = string.Empty;
    public string Neighborhood { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Number { get; private set; } = string.Empty;
    public string ZipCode { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    
    public override string ToString()
    {
        return $"{Street}, {Number}, {Neighborhood}, {City}, {State}, {ZipCode}";
    }
}