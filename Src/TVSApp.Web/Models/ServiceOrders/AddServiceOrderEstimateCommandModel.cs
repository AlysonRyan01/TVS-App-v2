using TVS_App.Domain.Enums;

namespace TVSApp.Web.Models.ServiceOrders;

public class AddServiceOrderEstimateCommandModel
{
    public long ServiceOrderId { get; set; }
    public string Guarantee { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public decimal PartCost { get; set; }
    public decimal LaborCost { get; set; }
    public ERepairResult RepairResult { get; set; }
    public string EstimateMessage { get; set; } = string.Empty;
}