using TVS_App.Application.Observability;

namespace TVSApp.Tests.Application;

[TestClass]
public class LogMaskTests
{
    [TestMethod]
    [DataRow("41999990000", "41*****0000")]
    [DataRow("5541999990000", "55*******0000")]
    [DataRow("(41) 99999-0000", "41*****0000")]
    [DataRow("12345", "***")]
    [DataRow("", "")]
    [DataRow(null, "")]
    public void Phone_KeepsOnlyFirstTwoAndLastFourDigits(string? phone, string expected)
    {
        Assert.AreEqual(expected, LogMask.Phone(phone));
    }

    [TestMethod]
    [DataRow("maria@loja.com", "m***@loja.com")]
    [DataRow("a@b.com", "a***@b.com")]
    [DataRow("sem-arroba", "***")]
    [DataRow("", "")]
    [DataRow(null, "")]
    public void Email_KeepsFirstLetterAndDomain(string? email, string expected)
    {
        Assert.AreEqual(expected, LogMask.Email(email));
    }
}
