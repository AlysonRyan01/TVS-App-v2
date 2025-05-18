using TVS_App.Domain.Exceptions;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVS_App.Tests.ValueObjects;

[TestClass]
public class PhoneTests
{
    [TestMethod]
    public void deve_retornar_true_quando_o_phone_for_correto() 
    {
        var correctPhone = new Phone("41997561468");

        Assert.IsNotNull(correctPhone);
    }
}
