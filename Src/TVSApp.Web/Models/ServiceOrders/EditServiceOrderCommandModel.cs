using TVS_App.Domain.Enums;

namespace TVSApp.Web.Models.ServiceOrders;

public class EditServiceOrderCommandModel
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Phone2 { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Defect { get; set; } = string.Empty;
    public string Accessories { get; set; } = string.Empty;
    public EProduct Type { get; set; }
    public string Location { get; set; } = string.Empty;
    public EEnterprise Enterprise { get; set; }
    public DateTime? EntryDate { get; set; }
    public DateTime? InspectionDate { get; set; }
    public DateTime? ResponseDate { get; set; }
    public DateTime? RepairDate { get; set; }
    public DateTime? PurchasePartDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? Solution { get; set; }
    public string? Guarantee { get; set; }
    public string? EstimateMessage { get; set; }
    public decimal PartCost { get; set; }
    public decimal LaborCost { get; set; }
    public EServiceOrderStatus ServiceOrderStatus { get; set; }
    public ERepairStatus RepairStatus { get; set; }
    public ERepairResult? RepairResult { get; set; }
}