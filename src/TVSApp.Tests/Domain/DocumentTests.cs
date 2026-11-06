using TVS_App.Domain.Enums;
using TVS_App.Domain.Exceptions;
using TVS_App.Domain.ValueObjects.Customer;

namespace TVSApp.Tests.Domain;

[TestClass]
public class DocumentTests
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void FromOptional_Empty_ReturnsNull(string? value)
    {
        Assert.IsNull(Document.FromOptional(value));
    }

    [TestMethod]
    public void Cpf_WithMask_IsNormalizedAndFormatted()
    {
        var document = new Document("123.456.789-09");

        Assert.AreEqual("12345678909", document.Number);
        Assert.AreEqual(EDocumentType.Cpf, document.Type);
        Assert.AreEqual("123.456.789-09", document.ToString());
    }

    [TestMethod]
    public void NumericCnpj_IsNormalizedAndFormatted()
    {
        var document = new Document("11.222.333/0001-81");

        Assert.AreEqual("11222333000181", document.Number);
        Assert.AreEqual(EDocumentType.Cnpj, document.Type);
        Assert.AreEqual("11.222.333/0001-81", document.ToString());
    }

    [TestMethod]
    public void AlphanumericCnpj_IsAcceptedInUpperCase()
    {
        var document = new Document("12.abc.345/01de-35");

        Assert.AreEqual("12ABC34501DE35", document.Number);
        Assert.AreEqual(EDocumentType.Cnpj, document.Type);
        Assert.AreEqual("12.ABC.345/01DE-35", document.ToString());
    }

    [TestMethod]
    [DataRow("123")]
    [DataRow("1234567890A")]
    [DataRow("12ABC34501DEXX")]
    [DataRow("123456789012345")]
    public void Invalid_Throws(string value)
    {
        var ex = Assert.ThrowsException<ValueObjectException<Document>>(() => new Document(value));

        Assert.AreEqual("Informe um CPF (11 dígitos) ou um CNPJ (14 caracteres).", ex.Message);
    }
}
