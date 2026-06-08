using Microsoft.EntityFrameworkCore;
using TVS_App.Application.DTOs.Dashboard;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Domain.Enums;
using TVS_App.Infrastructure.Data;

namespace TVS_App.Infrastructure.Services;

public class MetricsService : IMetricsService
{
    private readonly ApplicationDataContext _context;

    public MetricsService(ApplicationDataContext context)
    {
        _context = context;
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(int month, int year)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var orders = await _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.EntryDate >= start && x.EntryDate < end)
            .ToListAsync();

        var deliveredOrders = await _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.DeliveryDate.HasValue && x.DeliveryDate >= start && x.DeliveryDate < end)
            .ToListAsync();

        return BuildMetrics(orders, deliveredOrders);
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsByRangeAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        var orders = await _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.EntryDate >= start && x.EntryDate < end)
            .ToListAsync();

        var deliveredOrders = await _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.DeliveryDate.HasValue && x.DeliveryDate >= start && x.DeliveryDate < end)
            .ToListAsync();

        return BuildMetrics(orders, deliveredOrders);
    }

    private static DashboardMetricsDto BuildMetrics(List<TVS_App.Domain.Entities.ServiceOrder> orders, List<TVS_App.Domain.Entities.ServiceOrder> deliveredOrders)
    {
        var repairs = orders.Where(x => x.Product.Defect != "VENDA").ToList();
        var sales = orders.Where(x => x.Product.Defect == "VENDA").ToList();
        var deliveredRepairs = deliveredOrders.Where(x => x.Product.Defect != "VENDA").ToList();
        var deliveredSales = deliveredOrders.Where(x => x.Product.Defect == "VENDA").ToList();

        // ── KPIs gerais ────────────────────────────────────────────────────────
        var totalRevenue = deliveredRepairs
            .Where(x => x.RepairStatus == ERepairStatus.Approved && x.RepairResult == ERepairResult.Repair)
            .Sum(x => x.TotalAmount);

        var approved = repairs.Count(x => x.RepairStatus == ERepairStatus.Approved);
        var disapproved = repairs.Count(x => x.RepairStatus == ERepairStatus.Disapproved);
        var approvalRate = (approved + disapproved) > 0
            ? Math.Round(approved * 100.0 / (approved + disapproved), 1)
            : 0.0;

        var repairsWithDelivery = repairs.Where(x => x.DeliveryDate.HasValue).ToList();
        var avgCycleDays = repairsWithDelivery.Count > 0
            ? Math.Round(repairsWithDelivery.Average(x => (x.DeliveryDate!.Value - x.EntryDate).TotalDays), 1)
            : 0.0;

        // ── KPIs de vendas ─────────────────────────────────────────────────────
        var totalSalesRevenue = deliveredSales.Sum(x => x.TotalAmount);

        var pendingDeliveryRevenue = repairs
            .Where(x => x.RepairStatus == ERepairStatus.Approved
                        && x.RepairResult == ERepairResult.Repair
                        && x.ServiceOrderStatus != EServiceOrderStatus.Delivered)
            .Sum(x => x.TotalAmount);

        // ── Gráfico: ordens por status ─────────────────────────────────────────
        var statusLabels = new List<string>();
        var statusValues = new List<double>();
        foreach (var g in orders.GroupBy(x => x.ServiceOrderStatus).OrderBy(g => (int)g.Key))
        {
            statusLabels.Add(GetStatusLabel(g.Key));
            statusValues.Add(g.Count());
        }

        // ── Gráfico: reparos por tipo de produto ───────────────────────────────
        var repairsByProductLabels = new List<string>();
        var repairsByProductValues = new List<double>();
        foreach (var g in repairs.GroupBy(x => x.Product.Type).OrderByDescending(g => g.Count()))
        {
            repairsByProductLabels.Add(GetProductLabel(g.Key));
            repairsByProductValues.Add(g.Count());
        }

        // ── Gráfico: vendas por tipo de produto ────────────────────────────────
        var salesByProductLabels = new List<string>();
        var salesByProductValues = new List<double>();
        foreach (var g in sales.GroupBy(x => x.Product.Type).OrderByDescending(g => g.Count()))
        {
            salesByProductLabels.Add(GetProductLabel(g.Key));
            salesByProductValues.Add(g.Count());
        }

        // ── Gráfico: reparos por empresa ───────────────────────────────────────
        var repairsByEnterpriseLabels = new List<string>();
        var repairsByEnterpriseValues = new List<double>();
        foreach (var g in repairs.GroupBy(x => x.Enterprise).OrderByDescending(g => g.Count()))
        {
            repairsByEnterpriseLabels.Add(GetEnterpriseLabel(g.Key));
            repairsByEnterpriseValues.Add(g.Count());
        }

        // ── Gráfico: vendas por empresa ────────────────────────────────────────
        var salesByEnterpriseLabels = new List<string>();
        var salesByEnterpriseValues = new List<double>();
        foreach (var g in sales.GroupBy(x => x.Enterprise).OrderByDescending(g => g.Count()))
        {
            salesByEnterpriseLabels.Add(GetEnterpriseLabel(g.Key));
            salesByEnterpriseValues.Add(g.Count());
        }

        // ── Gráfico: resultado dos reparos ─────────────────────────────────────
        var repairResultLabels = new List<string>();
        var repairResultValues = new List<double>();
        foreach (var g in repairs
                     .Where(x => x.RepairResult.HasValue)
                     .GroupBy(x => x.RepairResult!.Value)
                     .OrderByDescending(g => g.Count()))
        {
            repairResultLabels.Add(GetRepairResultLabel(g.Key));
            repairResultValues.Add(g.Count());
        }

        return new DashboardMetricsDto
        {
            TotalOrders = repairs.Count,
            ActiveOrders = repairs.Count(x => x.ServiceOrderStatus != EServiceOrderStatus.Delivered),
            TotalRevenue = totalRevenue,
            ApprovalRate = approvalRate,
            AverageCycleDays = avgCycleDays,

            TotalSales = sales.Count,
            TotalSalesRevenue = totalSalesRevenue,
            PendingDeliveryRevenue = pendingDeliveryRevenue,

            OrdersByStatusLabels = statusLabels,
            OrdersByStatusValues = statusValues,

            RepairsByProductTypeLabels = repairsByProductLabels,
            RepairsByProductTypeValues = repairsByProductValues,

            SalesByProductTypeLabels = salesByProductLabels,
            SalesByProductTypeValues = salesByProductValues,

            RepairsByEnterpriseLabels = repairsByEnterpriseLabels,
            RepairsByEnterpriseValues = repairsByEnterpriseValues,

            SalesByEnterpriseLabels = salesByEnterpriseLabels,
            SalesByEnterpriseValues = salesByEnterpriseValues,

            RepairResultLabels = repairResultLabels,
            RepairResultValues = repairResultValues,
        };
    }

    private static string GetStatusLabel(EServiceOrderStatus status) => status switch
    {
        EServiceOrderStatus.Entered => "Entrada",
        EServiceOrderStatus.Evaluated => "Avaliado",
        EServiceOrderStatus.Repaired => "Consertado",
        EServiceOrderStatus.Delivered => "Entregue",
        EServiceOrderStatus.OrderPart => "Aguardando Peça",
        EServiceOrderStatus.ReadyToPickUp => "Pronto p/ Retirada",
        _ => status.ToString()
    };

    private static string GetProductLabel(EProduct product) => product switch
    {
        EProduct.Tv => "TV",
        EProduct.TvBox => "TV Box",
        EProduct.ControleRemoto => "Controle Remoto",
        EProduct.Som => "Som",
        EProduct.CaixaAcustica => "Caixa Acústica",
        EProduct.Microondas => "Microondas",
        EProduct.Fonte => "Fonte",
        EProduct.Bases => "Bases",
        EProduct.PlacaFonte => "Placa Fonte",
        EProduct.PlacaPrincipal => "Placa Principal",
        _ => product.ToString()
    };

    private static string GetEnterpriseLabel(EEnterprise enterprise) => enterprise switch
    {
        EEnterprise.Particular => "Particular",
        EEnterprise.Cocel => "Cocel",
        EEnterprise.Seguradora => "Seguradora",
        EEnterprise.PhilcoBritania => "Philco/Britania",
        EEnterprise.Copel => "Copel",
        _ => enterprise.ToString()
    };

    private static string GetRepairResultLabel(ERepairResult result) => result switch
    {
        ERepairResult.Repair => "Tem conserto",
        ERepairResult.Unrepaired => "Não tem conserto",
        ERepairResult.NoDefectFound => "Não apresentou defeito",
        _ => result.ToString()
    };
}
