using TVS_App.Application.Services;
using TVS_App.Domain.Entities;
using TVS_App.Domain.ValueObjects.ServiceOrder;
using TVSApp.Tests.Handlers.Fakes;

namespace TVSApp.Tests.Application;

[TestClass]
public class PaymentReceiptSummaryTests
{
    private static ServiceOrder OrderWithTotal(decimal total)
    {
        var order = ServiceOrderFactory.Create();
        order.EditLaborCost(new LaborCost(total));
        return order;
    }

    private static Payment Pay(ServiceOrder order, long id, decimal amount, DateTime date)
    {
        var payment = new Payment(amount, date, order.Id) { Id = id };
        order.AddPayment(payment);
        return payment;
    }

    [TestMethod]
    public void SumsOnlyPaymentsUntilThisOne()
    {
        var order = OrderWithTotal(500m);
        Pay(order, 1, 100m, new DateTime(2026, 11, 1));
        var second = Pay(order, 2, 150m, new DateTime(2026, 11, 3));
        Pay(order, 3, 200m, new DateTime(2026, 11, 5));

        var summary = PaymentReceiptSummary.For(order, second);

        Assert.AreEqual(500m, summary.ServiceOrderTotal);
        Assert.AreEqual(250m, summary.PaidUntilThisPayment);
        Assert.AreEqual(250m, summary.Balance);
    }

    [TestMethod]
    public void SameDate_TieBreaksById()
    {
        var order = OrderWithTotal(300m);
        var first = Pay(order, 7, 100m, new DateTime(2026, 11, 2));
        Pay(order, 8, 50m, new DateTime(2026, 11, 2));

        var summary = PaymentReceiptSummary.For(order, first);

        Assert.AreEqual(100m, summary.PaidUntilThisPayment);
        Assert.AreEqual(200m, summary.Balance);
    }

    [TestMethod]
    public void Overpaid_BalanceIsNegative()
    {
        var order = OrderWithTotal(100m);
        var payment = Pay(order, 1, 130m, new DateTime(2026, 11, 2));

        var summary = PaymentReceiptSummary.For(order, payment);

        Assert.AreEqual(-30m, summary.Balance);
    }
}
