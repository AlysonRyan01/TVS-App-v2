using System.Text.Json.Serialization;
using TVS_App.Domain.Enums;

namespace TVS_App.Domain.ValueObjects.ServiceOrder;

public class Product : ValueObject
{
    public Product() { }
    
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

    [JsonPropertyName("brand")]
    [JsonInclude]
    public string Brand { get; private set; } = null!;
    [JsonPropertyName("model")]
    [JsonInclude]
    public string Model { get; private set; } = null!;
    [JsonPropertyName("serialNumber")]
    [JsonInclude]
    public string SerialNumber { get; private set; } = null!;
    [JsonPropertyName("defect")]
    [JsonInclude]
    public string? Defect { get; private set; }
    [JsonPropertyName("accessories")]
    [JsonInclude]
    public string? Accessories { get; private set; }
    [JsonPropertyName("type")]
    [JsonInclude]
    public EProduct Type { get; private set; }
    [JsonPropertyName("location")]
    [JsonInclude]
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
