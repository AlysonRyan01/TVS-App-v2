using System.Text.Json.Serialization;

namespace TVS_App.Domain.ValueObjects.ServiceOrder;

public class Solution : ValueObject
{
    public Solution() { }
    
    [JsonConstructor]
    public Solution(string serviceOrderSolution)
    {
        ServiceOrderSolution = serviceOrderSolution;
    }

    [JsonPropertyName("serviceOrderSolution")]
    public string ServiceOrderSolution { get; private set; } = string.Empty;
}