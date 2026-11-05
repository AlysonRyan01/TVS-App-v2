using TVS_App.Domain.Enums;
using TVSApp.Tests.Handlers.Fakes;

namespace TVSApp.Tests.Domain;

[TestClass]
public class ServiceOrderDeliveryTests
{
    [TestMethod]
    public void AddDelivery_SetsDeliveredAndDeliveryDate()
    {
        var serviceOrder = ServiceOrderFactory.Create();

        serviceOrder.AddDelivery();

        Assert.AreEqual(EServiceOrderStatus.Delivered, serviceOrder.ServiceOrderStatus);
        Assert.IsNotNull(serviceOrder.DeliveryDate);
    }
}
