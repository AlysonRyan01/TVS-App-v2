namespace TVS_App.Application.DTOs.Dashboard;

public class DeliveredProductDto
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string TypeLabel { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string EnterpriseLabel { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PartPurchaseCost { get; set; }
}
