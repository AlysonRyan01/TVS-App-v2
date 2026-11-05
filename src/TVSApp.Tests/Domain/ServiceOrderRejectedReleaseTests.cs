using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Exceptions;
using TVSApp.Tests.Handlers.Fakes;

namespace TVSApp.Tests.Domain;

[TestClass]
public class ServiceOrderRejectedReleaseTests
{
    private static ServiceOrder AwaitingResponse()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        serviceOrder.AddEstimate("TROCA DA PLACA", "90 DIAS", 350m, ERepairResult.Repair, "msg");
        return serviceOrder;
    }

    [TestMethod]
    public void MarkEstimateRejectedAsReadyToPickUp_RejectedEstimate_MovesToReadyToPickUp()
    {
        var serviceOrder = AwaitingResponse();
        serviceOrder.RejectEstimate();

        serviceOrder.MarkEstimateRejectedAsReadyToPickUp();

        Assert.AreEqual(EServiceOrderStatus.ReadyToPickUp, serviceOrder.ServiceOrderStatus);
        Assert.AreEqual(ERepairStatus.Disapproved, serviceOrder.RepairStatus);
    }

    [TestMethod]
    public void MarkEstimateRejectedAsReadyToPickUp_NotRejected_Throws()
    {
        var serviceOrder = AwaitingResponse();

        var ex = Assert.ThrowsException<EntityException<ServiceOrder>>(
            () => serviceOrder.MarkEstimateRejectedAsReadyToPickUp());

        Assert.AreEqual("O orçamento precisa estar rejeitado para marcar como pronto para entrega", ex.Message);
    }
}
