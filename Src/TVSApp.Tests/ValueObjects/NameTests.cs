using TVS_App.Domain.Exceptions;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Tests.ValueObjects;

[TestClass]
public class NameTests
{
    [TestMethod]
    [ExpectedException(typeof(ValueObjectException<Name>))]
    public void deve_retornar_exception_quando_o_nome_for_vazio()
    {
        var emptyName = new Name("");
    }

    [TestMethod]
    public void deve_retornar_true_quando_o_nome_for_correto() 
    {
        var correctName = new Name("Alyson");

        Assert.IsNotNull(correctName);
    }
}
