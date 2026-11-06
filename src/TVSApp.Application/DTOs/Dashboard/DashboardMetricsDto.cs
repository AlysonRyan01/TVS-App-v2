namespace TVS_App.Application.DTOs.Dashboard;

public class DashboardMetricsDto
{
    public int TotalOrders { get; set; }
    public int ActiveOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public double ApprovalRate { get; set; }
    public double AverageCycleDays { get; set; }

    public int TotalSales { get; set; }
    public decimal TotalSalesRevenue { get; set; }

    public decimal PendingDeliveryRevenue { get; set; }

    public decimal TotalPartsSpend { get; set; }

    public decimal TotalValue { get; set; }
    public decimal NetTotal { get; set; }

    public List<DeliveredProductDto> DeliveredProducts { get; set; } = [];

    public List<string> OrdersByStatusLabels { get; set; } = [];
    public List<double> OrdersByStatusValues { get; set; } = [];

    public List<string> RepairsByProductTypeLabels { get; set; } = [];
    public List<double> RepairsByProductTypeValues { get; set; } = [];

    public List<string> SalesByProductTypeLabels { get; set; } = [];
    public List<double> SalesByProductTypeValues { get; set; } = [];

    public List<string> RepairsByEnterpriseLabels { get; set; } = [];
    public List<double> RepairsByEnterpriseValues { get; set; } = [];

    public List<string> SalesByEnterpriseLabels { get; set; } = [];
    public List<double> SalesByEnterpriseValues { get; set; } = [];

    public List<string> RepairResultLabels { get; set; } = [];
    public List<double> RepairResultValues { get; set; } = [];
}
