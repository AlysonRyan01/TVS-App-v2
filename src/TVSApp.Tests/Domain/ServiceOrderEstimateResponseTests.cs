using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Exceptions;
using TVSApp.Tests.Handlers.Fakes;

namespace TVSApp.Tests.Domain;

[TestClass]
public class ServiceOrderEstimateResponseTests
{
    private static ServiceOrder AwaitingResponse()
    {
        var serviceOrder = ServiceOrderFactory.Create(EEnterprise.Particular);
        serviceOrder.AddEstimate("TROCA DA PLACA", "90 DIAS", 350m, ERepairResult.Repair, "msg");
        return serviceOrder;
    }

    [TestMethod]
    public void IsAwaitingEstimateResponse_NewServiceOrder_IsFalse()
    {
        var serviceOrder = ServiceOrderFactory.Create();

        Assert.IsFalse(serviceOrder.IsAwaitingEstimateResponse);
    }

    [TestMethod]
    public void IsAwaitingEstimateResponse_AfterEstimateWithRepair_IsTrue()
    {
        Assert.IsTrue(AwaitingResponse().IsAwaitingEstimateResponse);
    }

    [TestMethod]
    public void ApproveEstimate_AwaitingResponse_SetsApprovedAndResponseDate()
    {
        var serviceOrder = AwaitingResponse();

        serviceOrder.ApproveEstimate();

        Assert.AreEqual(ERepairStatus.Approved, serviceOrder.RepairStatus);
        Assert.IsNotNull(serviceOrder.ResponseDate);
        Assert.IsFalse(serviceOrder.IsAwaitingEstimateResponse);
    }

    [TestMethod]
    public void RejectEstimate_AwaitingResponse_SetsDisapprovedAndResponseDate()
    {
        var serviceOrder = AwaitingResponse();

        serviceOrder.RejectEstimate();

        Assert.AreEqual(ERepairStatus.Disapproved, serviceOrder.RepairStatus);
        Assert.IsNotNull(serviceOrder.ResponseDate);
        Assert.IsFalse(serviceOrder.IsAwaitingEstimateResponse);
    }

    [TestMethod]
    [DataRow(true, true)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(false, false)]
    public void RespondTwice_Throws(bool firstApprove, bool secondApprove)
    {
        var serviceOrder = AwaitingResponse();
        if (firstApprove) serviceOrder.ApproveEstimate(); else serviceOrder.RejectEstimate();

        var ex = Assert.ThrowsException<EntityException<ServiceOrder>>(() =>
        {
            if (secondApprove) serviceOrder.ApproveEstimate(); else serviceOrder.RejectEstimate();
        });

        Assert.AreEqual("Esse orçamento já foi respondido.", ex.Message);
    }

    [TestMethod]
    public void ApproveEstimate_WithoutEstimate_Throws()
    {
        var serviceOrder = ServiceOrderFactory.Create();

        Assert.ThrowsException<EntityException<ServiceOrder>>(() => serviceOrder.ApproveEstimate());
    }
}
