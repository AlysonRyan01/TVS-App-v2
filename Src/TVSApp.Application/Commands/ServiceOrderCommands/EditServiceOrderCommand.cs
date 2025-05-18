using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Application.Commands.ServiceOrderCommands;

public class EditServiceOrderCommand : ICommand
{
    public long CustomerId { get; set; }
    
    public Customer Customer { get; set; } = null!;
    
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
    public string? Defect { get; set; }
    public string? Accessories { get; set; }
    public EProduct Type { get; set; }
    public string Location { get; set; } = string.Empty;
        
    public EEnterprise Enterprise { get; set; }
    public DateTime? DeliveryDate { get; set; }
    
    public string? Solution { get; set; }
    
    public string? Guarantee { get; set; }
    
    public string? EstimateMessage { get; set; }
    
    public decimal PartCost { get; set; }
    
    public decimal LaborCost { get; set; }
    
    public EServiceOrderStatus ServiceOrderStatus { get; set; }
    
    public ERepairStatus RepairStatus { get; set; }
    
    public ERepairResult RepairResult { get; set; }
    
    public void Validate()
    {
        
    }
}