namespace TVS_App.Domain.ValueObjects.ServiceOrder;

public class Solution : ValueObject
{
    protected Solution() { }
    
    public Solution(string serviceOrderSolution)
    {
        ServiceOrderSolution = serviceOrderSolution;
    }
    
    public string ServiceOrderSolution { get; private set; } = string.Empty;
}