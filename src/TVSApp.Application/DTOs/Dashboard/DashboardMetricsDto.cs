namespace TVS_App.Application.DTOs.Dashboard;

public class DashboardMetricsDto
{
    // KPIs gerais — mês atual
    public int TotalOrders { get; set; }
    public int ActiveOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public double ApprovalRate { get; set; }
    public double AverageCycleDays { get; set; }

    // KPIs de vendas — mês atual
    public int TotalSales { get; set; }
    public decimal TotalSalesRevenue { get; set; }

    // KPI: reparos aprovados ainda não entregues
    public decimal PendingDeliveryRevenue { get; set; }

    // KPI: gasto com peças (produtos entregues no período)
    public decimal TotalPartsSpend { get; set; }

    // Lista: produtos entregues no período (reparos + vendas)
    public List<DeliveredProductDto> DeliveredProducts { get; set; } = [];

    // Gráfico: ordens por status
    public List<string> OrdersByStatusLabels { get; set; } = [];
    public List<double> OrdersByStatusValues { get; set; } = [];

    // Gráfico: reparos por tipo de produto
    public List<string> RepairsByProductTypeLabels { get; set; } = [];
    public List<double> RepairsByProductTypeValues { get; set; } = [];

    // Gráfico: vendas por tipo de produto
    public List<string> SalesByProductTypeLabels { get; set; } = [];
    public List<double> SalesByProductTypeValues { get; set; } = [];

    // Gráfico: reparos por empresa
    public List<string> RepairsByEnterpriseLabels { get; set; } = [];
    public List<double> RepairsByEnterpriseValues { get; set; } = [];

    // Gráfico: vendas por empresa
    public List<string> SalesByEnterpriseLabels { get; set; } = [];
    public List<double> SalesByEnterpriseValues { get; set; } = [];

    // Gráfico: resultado dos reparos
    public List<string> RepairResultLabels { get; set; } = [];
    public List<double> RepairResultValues { get; set; } = [];
}
