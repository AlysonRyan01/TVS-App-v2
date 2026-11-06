using TVS_App.Application.Services;

namespace TVSApp.Tests.Application;

[TestClass]
public class ValorPorExtensoTests
{
    [TestMethod]
    [DataRow("1", "um real")]
    [DataRow("2", "dois reais")]
    [DataRow("0.50", "cinquenta centavos")]
    [DataRow("0.01", "um centavo")]
    [DataRow("150.30", "cento e cinquenta reais e trinta centavos")]
    [DataRow("100", "cem reais")]
    [DataRow("101", "cento e um reais")]
    [DataRow("2500", "dois mil e quinhentos reais")]
    [DataRow("1234.56", "mil duzentos e trinta e quatro reais e cinquenta e seis centavos")]
    [DataRow("1000000", "um milhão de reais")]
    [DataRow("2000001", "dois milhões e um reais")]
    [DataRow("15", "quinze reais")]
    [DataRow("1100", "mil e cem reais")]
    [DataRow("1001.01", "mil e um reais e um centavo")]
    public void Format_ReturnsPortugueseWords(string value, string expected)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual(expected, ValorPorExtenso.Format(amount));
    }

    [TestMethod]
    public void Format_Zero_IsZeroReais()
    {
        Assert.AreEqual("zero real", ValorPorExtenso.Format(0m));
    }
}
