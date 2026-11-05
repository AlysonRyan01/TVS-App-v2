using TVS_App.Domain.Enums;
using TVSApp.Tests.Handlers.Fakes;

namespace TVSApp.Tests.Domain;

[TestClass]
public class ServiceOrderPartPurchaseTests
{
    [TestMethod]
    public void AddPurchasedPart_ApprovedServiceOrder_MovesToOrderPartWithPurchaseDate()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        serviceOrder.AddEstimate("TROCA DA PLACA", "90 DIAS", 350m, ERepairResult.Repair, "msg");
        serviceOrder.ApproveEstimate();

        serviceOrder.AddPurchasedPart();

        Assert.AreEqual(EServiceOrderStatus.OrderPart, serviceOrder.ServiceOrderStatus);
        Assert.IsNotNull(serviceOrder.PurchasePartDate);
    }
}
