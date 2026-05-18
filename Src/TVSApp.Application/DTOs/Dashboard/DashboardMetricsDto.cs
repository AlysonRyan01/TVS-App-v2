namespace TVS_App.Application.DTOs.Dashboard;

public class DashboardMetricsDto
{
    // KPIs gerais
    public int TotalOrders { get; set; }
    public int TotalOrdersThisMonth { get; set; }
    public int ActiveOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public double ApprovalRate { get; set; }
    public double AverageCycleDays { get; set; }

    // KPIs de vendas
    public int TotalSales { get; set; }
    public int TotalSalesThisMonth { get; set; }
    public decimal TotalSalesRevenue { get; set; }
    public decimal SalesRevenueThisMonth { get; set; }

    // Gráfico: ordens por status
    public List<string> OrdersByStatusLabels { get; set; } = [];
    public List<double> OrdersByStatusValues { get; set; } = [];

    // Gráfico: receita de reparos por mês
    public List<string> RevenueByMonthLabels { get; set; } = [];
    public List<double> RevenueByMonthValues { get; set; } = [];

    // Gráfico: ordens de reparo criadas por mês
    public List<string> OrdersCreatedPerMonthLabels { get; set; } = [];
    public List<double> OrdersCreatedPerMonthValues { get; set; } = [];

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

    // Gráfico: receita de vendas por mês
    public List<string> SalesRevenueByMonthLabels { get; set; } = [];
    public List<double> SalesRevenueByMonthValues { get; set; } = [];

    // Tabela: top clientes
    public List<TopCustomerDto> TopCustomers { get; set; } = [];
}

public class TopCustomerDto
{
    public string CustomerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}
