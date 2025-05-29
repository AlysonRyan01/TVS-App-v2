using TVS_App.Domain.Enums;

namespace TVS_App.Domain.ValueObjects.ServiceOrder;

public class Product : ValueObject
{
    protected Product() { }
    
    public Product(string brand, string model, string serialNumber,
        string? defect, string? accessories, EProduct type)
    {
        Brand = brand;
        Model = model;
        SerialNumber = serialNumber;
        Defect = defect;
        Accessories = accessories;
        Type = type;
    }
    
    public string Brand { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public string SerialNumber { get; private set; } = null!;
    public string? Defect { get; private set; }
    public string? Accessories { get; private set; }
    public EProduct Type { get; private set; }
    public string Location { get; private set; } = string.Empty;

    public void UpdateProduct(string brand, string model, string serialNumber, string defect, string accessories, EProduct type)
    {
        Brand = brand;
        Model = model;
        SerialNumber = serialNumber;
        Defect = defect;
        Accessories = accessories;
        Type = type;
    }

    public void AddLocation(string location)
    {
        Location = location;
    }
}
