using TVS_App.Domain.Exceptions;
using TVS_App.Domain.ValueObjects.ServiceOrder;

namespace TVS_App.Tests.ValueObjects;

[TestClass]
public class LaborCostTests
{
    [TestMethod]
    [ExpectedException(typeof(ValueObjectException<LaborCost>))]
    public void deve_retornar_exception_quando_o_valor_for_menor_que_0()
    {
        var incorrectLabor = new LaborCost(-5);
    }

    [TestMethod]
    public void deve_retornar_true_quando_o_valor_correto()
    {
        var corretLaborCost = new LaborCost(200);
        Assert.AreEqual(200, corretLaborCost.ServiceOrderLaborCost);
    }
}