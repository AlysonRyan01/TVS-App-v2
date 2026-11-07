using TVS_App.Application.Search;

namespace TVSApp.Tests.Application;

[TestClass]
public class CustomerNameNormalizerTests
{
    [TestMethod]
    [DataRow("José Conceição", "JOSE CONCEICAO")]
    [DataRow("  joão   da  silva ", "JOAO DA SILVA")]
    [DataRow("ÂÃÀÁÄ êëè ñ", "AAAAA EEE N")]
    [DataRow("50%_off", "50%_OFF")]
    [DataRow("MARIA\tAPARECIDA", "MARIA APARECIDA")]
    [DataRow("JOAO DA SILVA", "JOAO DA SILVA")]
    public void Normalize_RemovesAccentsUppercasesAndCollapsesSpaces(string input, string expected)
    {
        Assert.AreEqual(expected, CustomerNameNormalizer.Normalize(input));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Normalize_EmptyInput_ReturnsEmpty(string? input)
    {
        Assert.AreEqual(string.Empty, CustomerNameNormalizer.Normalize(input));
    }

    [TestMethod]
    public void Entry_Create_KeepsOriginalNameAndComputesKey()
    {
        var entry = CustomerSearchEntry.Create(7, "José Conceição", null);

        Assert.AreEqual(7, entry.Id);
        Assert.AreEqual("José Conceição", entry.Name);
        Assert.AreEqual(string.Empty, entry.Phone);
        Assert.AreEqual("JOSE CONCEICAO", entry.SearchKey);
    }
}
