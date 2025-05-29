using TVS_App.Domain.Enums;

namespace TVS_App.Application.DTOs.ServiceOrder;

public record ServiceOrderDto
{
    public long Id { get; init; }
    
    public long CustomerId { get; set; }
    public string Name { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string Neighborhood { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Phone2 { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string Cpf { get; init; } = string.Empty;
    
    public string Brand { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string? Defect { get; init; }
    public string? Accessories { get; init; } 
    public EProduct Type { get; init; }
    public string Location { get; init; } = string.Empty;
    
    public EEnterprise Enterprise { get; init; }
    public DateTime EntryDate { get; init; }
    public DateTime? InspectionDate { get; init; }
    public DateTime? ResponseDate { get; init; }
    public DateTime? RepairDate { get; init; }
    public DateTime? PurchasePartDate { get; init; }
    public DateTime? DeliveryDate { get; init; }
    
    public string? Solution { get; init; } = string.Empty;
    public string? EstimateMessage { get; init; }
    public string? Guarantee { get; init; }
    public decimal PartCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal TotalAmount => PartCost + LaborCost;
    
    public EServiceOrderStatus ServiceOrderStatus { get; init; }
    public ERepairStatus RepairStatus { get; init; }
    public ERepairResult? RepairResult { get; init; }
    
    public string FormattedAddress => 
        $"{Street}, {Number} - {Neighborhood}, {City} - {State}, {ZipCode}".ToUpper();
}