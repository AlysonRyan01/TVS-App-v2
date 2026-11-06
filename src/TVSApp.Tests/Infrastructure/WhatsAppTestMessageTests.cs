using System.Net;
using System.Text.Json;

namespace TVSApp.Tests.Infrastructure;

[TestClass]
public class WhatsAppTestMessageTests
{
    [TestMethod]
    public async Task SendsMarkedTestMessage_ToConfiguredNumber()
    {
        var handler = StubHandler.Json(HttpStatusCode.Created, "{}");
        var service = WhatsAppServiceTestKit.Create(handler);

        var result = await service.SendTestMessageAsync("(41) 99999-0000");

        Assert.IsTrue(result.IsSuccess);
        var (request, body) = handler.Calls.Single();
        Assert.AreEqual("http://evolution.test/message/sendText/tvs", request.RequestUri!.ToString());
        using var json = JsonDocument.Parse(body);
        Assert.AreEqual("5541999990000", json.RootElement.GetProperty("number").GetString());
        var text = json.RootElement.GetProperty("text").GetString()!;
        StringAssert.Contains(text, "Mensagem de teste");
        StringAssert.Contains(text, "TVS Eletrônica");
    }

    [TestMethod]
    public async Task EvolutionError_ReturnsFailureWithReason()
    {
        var service = WhatsAppServiceTestKit.Create(StubHandler.Json(HttpStatusCode.InternalServerError, """{"error":"instância desconectada"}"""));

        var result = await service.SendTestMessageAsync("41999990000");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(502, result.Code);
        StringAssert.StartsWith(result.Message, "Falha ao enviar a mensagem de teste:");
        StringAssert.Contains(result.Message, "instância desconectada");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public async Task WithoutNumber_IsRefused_AndDoesNotCallEvolution(string? phone)
    {
        var handler = StubHandler.Json(HttpStatusCode.OK, "{}");
        var service = WhatsAppServiceTestKit.Create(handler);

        var result = await service.SendTestMessageAsync(phone);

        Assert.AreEqual(400, result.Code);
        Assert.AreEqual("Configure WHATSAPP_TEST_PHONE no .env para usar o envio de teste.", result.Message);
        Assert.AreEqual(0, handler.Calls.Count);
    }

    [TestMethod]
    public async Task InDevelopment_StillSendsToConfiguredNumber()
    {
        var handler = StubHandler.Json(HttpStatusCode.OK, "{}");
        var service = WhatsAppServiceTestKit.Create(handler, environment: "Development");

        await service.SendTestMessageAsync("41988887777");

        using var json = JsonDocument.Parse(handler.Calls.Single().Body);
        Assert.AreEqual("5541988887777", json.RootElement.GetProperty("number").GetString());
    }
}
