namespace TVS_App.Domain.ValueObjects.Customer;

public class Phone : ValueObject
{
    protected Phone() { }
    
    public Phone(string customerPhone)
    {
        CustomerPhone = customerPhone;
    }
    
    public string CustomerPhone { get; private set; } = string.Empty;

    public override string ToString() => CustomerPhone;
}