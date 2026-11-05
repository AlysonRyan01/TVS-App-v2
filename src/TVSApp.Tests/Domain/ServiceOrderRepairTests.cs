using TVS_App.Domain.Entities;
using TVS_App.Domain.Enums;
using TVS_App.Domain.Exceptions;
using TVSApp.Tests.Handlers.Fakes;

namespace TVSApp.Tests.Domain;

[TestClass]
public class ServiceOrderRepairTests
{
    private static ServiceOrder AwaitingResponse()
    {
        var serviceOrder = ServiceOrderFactory.Create();
        serviceOrder.AddEstimate("TROCA DA PLACA", "90 DIAS", 350m, ERepairResult.Repair, "msg");
        return serviceOrder;
    }

    [TestMethod]
    public void ExecuteRepair_AwaitingPart_MovesToRepairedWithDate()
    {
        var serviceOrder = AwaitingResponse();
        serviceOrder.ApproveEstimate();
        serviceOrder.AddPurchasedPart();

        serviceOrder.ExecuteRepair();

        Assert.AreEqual(EServiceOrderStatus.Repaired, serviceOrder.ServiceOrderStatus);
        Assert.IsNotNull(serviceOrder.RepairDate);
    }

    [TestMethod]
    public void ExecuteRepair_AwaitingResponse_Throws()
    {
        var serviceOrder = AwaitingResponse();

        var ex = Assert.ThrowsException<EntityException<ServiceOrder>>(() => serviceOrder.ExecuteRepair());

        Assert.AreEqual("Não podemos executar o conserto pois a ordem de serviço não tem o status de aprovado", ex.Message);
    }
}
