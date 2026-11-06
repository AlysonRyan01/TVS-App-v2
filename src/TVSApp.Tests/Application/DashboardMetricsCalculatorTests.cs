using TVS_App.Application.Services;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.ValueObjects.ServiceOrder;
using TVSApp.Tests.Handlers.Fakes;

namespace TVSApp.Tests.Application;

[TestClass]
public class DashboardMetricsCalculatorTests
{
    private static readonly DateTime Entry = new(2026, 11, 2, 10, 0, 0);
    private static long _nextId = 100;

    /// <summary>OS de reparo com orçamento, valor e (opcionalmente) entrega.</summary>
    private static ServiceOrder Repair(
        ERepairStatus repairStatus = ERepairStatus.Approved,
        ERepairResult result = ERepairResult.Repair,
        EServiceOrderStatus status = EServiceOrderStatus.Repaired,
        decimal labor = 100m,
        DateTime? delivery = null,
        decimal partPurchase = 0m,
        string accessories = "")
    {
        var order = ServiceOrderFactory.Create();
        order.Id = _nextId++;
        order.Product.UpdateProduct("LG", "42LB", "SN", "NÃO LIGA", accessories, EProduct.Tv);
        order.EditEntryDate(Entry);
        order.EditRepairStatus(repairStatus);
        order.EditRepairResult(result);
        order.EditServiceOrderStatus(delivery.HasValue ? EServiceOrderStatus.Delivered : status);
        order.EditLaborCost(new LaborCost(labor));
        order.EditPartPurchaseCost(new PartPurchaseCost(partPurchase));
        order.EditDeliveryDate(delivery);
        return order;
    }

    /// <summary>OS de venda (defeito "VENDA"), entregue no ato, como o handler de venda.</summary>
    private static ServiceOrder Sale(decimal amount = 500m, EProduct type = EProduct.Tv)
    {
        var order = ServiceOrderFactory.Create();
        order.Id = _nextId++;
        order.Product.UpdateProduct("SAMSUNG", "UN50", "SN", "VENDA", "", type);
        order.EditEntryDate(Entry);
        order.EditPartCost(new PartCost(amount));
        order.EditServiceOrderStatus(EServiceOrderStatus.Delivered);
        order.EditRepairStatus(ERepairStatus.Approved);
        order.EditRepairResult(ERepairResult.Repair);
        order.EditDeliveryDate(Entry);
        return order;
    }

    private static TVS_App.Application.DTOs.Dashboard.DashboardMetricsDto Build(
        IEnumerable<ServiceOrder> orders, IEnumerable<ServiceOrder>? delivered = null)
    {
        var list = orders.ToList();
        return DashboardMetricsCalculator.Build(list, (delivered ?? list.Where(x => x.DeliveryDate.HasValue)).ToList());
    }

    [TestMethod]
    public void OrderCounts_IgnoreSales()
    {
        var metrics = Build([Repair(), Repair(delivery: Entry.AddDays(2)), Sale()]);

        Assert.AreEqual(2, metrics.TotalOrders);
        Assert.AreEqual(1, metrics.ActiveOrders);
        Assert.AreEqual(1, metrics.TotalSales);
    }

    [TestMethod]
    public void RepairRevenue_OnlyDeliveredApprovedWithRepair()
    {
        var metrics = Build([
            Repair(labor: 300m, delivery: Entry.AddDays(1)),                                       // conta
            Repair(labor: 999m),                                                                    // não entregue
            Repair(labor: 50m, result: ERepairResult.Unrepaired, delivery: Entry.AddDays(1)),       // sem conserto
            Sale(amount: 700m),                                                                     // venda
        ]);

        Assert.AreEqual(300m, metrics.TotalRevenue);
        Assert.AreEqual(700m, metrics.TotalSalesRevenue);
    }

    [TestMethod]
    public void ApprovalRate_ApprovedOverAnswered()
    {
        var metrics = Build([Repair(), Repair(), Repair(repairStatus: ERepairStatus.Disapproved), Repair(repairStatus: ERepairStatus.Waiting)]);

        Assert.AreEqual(66.7, metrics.ApprovalRate);
    }

    [TestMethod]
    public void ApprovalRate_NoAnsweredEstimates_IsZero()
    {
        var metrics = Build([Repair(repairStatus: ERepairStatus.Waiting)]);

        Assert.AreEqual(0.0, metrics.ApprovalRate);
    }

    [TestMethod]
    public void AverageCycle_IgnoresDeliveryBeforeEntry()
    {
        var metrics = Build([
            Repair(delivery: Entry.AddDays(2)),
            Repair(delivery: Entry.AddDays(4)),
            Repair(delivery: Entry.AddDays(-3)), // entrada alterada depois da entrega: fora da média
        ]);

        Assert.AreEqual(3.0, metrics.AverageCycleDays);
    }

    [TestMethod]
    public void PendingDelivery_ApprovedRepairNotDelivered()
    {
        var metrics = Build([Repair(labor: 200m), Repair(labor: 80m, repairStatus: ERepairStatus.Disapproved), Repair(labor: 50m, delivery: Entry)]);

        Assert.AreEqual(200m, metrics.PendingDeliveryRevenue);
    }

    [TestMethod]
    public void Totals_ValueAndNet()
    {
        var metrics = Build([
            Repair(labor: 300m, delivery: Entry.AddDays(1), partPurchase: 120m),
            Sale(amount: 700m),
        ]);

        Assert.AreEqual(120m, metrics.TotalPartsSpend);
        Assert.AreEqual(1000m, metrics.TotalValue);
        Assert.AreEqual(880m, metrics.NetTotal);
    }

    [TestMethod]
    public void Charts_IgnoreSales()
    {
        var metrics = Build([Repair(), Repair(result: ERepairResult.NoDefectFound), Sale(), Sale()]);

        Assert.AreEqual(2, metrics.OrdersByStatusValues.Sum());
        Assert.AreEqual(2, metrics.RepairResultValues.Sum());
        CollectionAssert.DoesNotContain(metrics.OrdersByStatusLabels, "Entregue");
    }

    [TestMethod]
    public void DeliveredProducts_LabelsAndOrder()
    {
        var older = Repair(delivery: Entry.AddDays(1), accessories: "CONTROLE");
        var newer = Sale();
        newer.EditDeliveryDate(Entry.AddDays(5));

        var metrics = Build([older, newer]);

        Assert.AreEqual(2, metrics.DeliveredProducts.Count);
        Assert.AreEqual("Venda", metrics.DeliveredProducts[0].StatusLabel);
        Assert.AreEqual("SAMSUNG UN50", metrics.DeliveredProducts[0].ProductLabel);
        Assert.AreEqual("Reparo", metrics.DeliveredProducts[1].StatusLabel);
        Assert.AreEqual("LG 42LB - CONTROLE", metrics.DeliveredProducts[1].ProductLabel);
    }

    [TestMethod]
    public void Empty_AllZeros()
    {
        var metrics = Build([]);

        Assert.AreEqual(0, metrics.TotalOrders);
        Assert.AreEqual(0m, metrics.TotalValue);
        Assert.AreEqual(0m, metrics.NetTotal);
        Assert.AreEqual(0.0, metrics.AverageCycleDays);
        Assert.AreEqual(0, metrics.DeliveredProducts.Count);
    }
}
