using System.Net;
using TVS_App.Application.DTOs;

namespace TVSApp.Tests.Infrastructure;

[TestClass]
public class WhatsAppConnectionStateTests
{
    [TestMethod]
    public async Task Open_IsConnected_AndCallsConnectionStateWithApiKey()
    {
        var handler = StubHandler.Json(HttpStatusCode.OK, """{"instance":{"instanceName":"tvs","state":"open"}}""");
        var service = WhatsAppServiceTestKit.Create(handler);

        var status = await service.GetConnectionStateAsync();

        Assert.AreEqual(WhatsAppConnectionState.Connected, status.State);
        var request = handler.Calls.Single().Request;
        Assert.AreEqual(HttpMethod.Get, request.Method);
        Assert.AreEqual("http://evolution.test/instance/connectionState/tvs", request.RequestUri!.ToString());
        Assert.AreEqual("chave", request.Headers.GetValues("apikey").Single());
    }

    [TestMethod]
    [DataRow("close")]
    [DataRow("connecting")]
    public async Task NotOpen_IsDisconnected_WithReason(string state)
    {
        var service = WhatsAppServiceTestKit.Create(StubHandler.Json(HttpStatusCode.OK, $$$"""{"instance":{"state":"{{{state}}}"}}"""));

        var status = await service.GetConnectionStateAsync();

        Assert.AreEqual(WhatsAppConnectionState.Disconnected, status.State);
        Assert.AreEqual($"instância em \"{state}\"", status.Reason);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task HttpError_IsUnavailable(HttpStatusCode code)
    {
        var service = WhatsAppServiceTestKit.Create(StubHandler.Json(code, """{"error":"x"}"""));

        var status = await service.GetConnectionStateAsync();

        Assert.AreEqual(WhatsAppConnectionState.Unavailable, status.State);
        StringAssert.Contains(status.Reason, ((int)code).ToString());
    }

    [TestMethod]
    public async Task ConnectionFailure_IsUnavailable()
    {
        var service = WhatsAppServiceTestKit.Create(StubHandler.Throws(new HttpRequestException("Connection refused")));

        var status = await service.GetConnectionStateAsync();

        Assert.AreEqual(WhatsAppConnectionState.Unavailable, status.State);
        StringAssert.Contains(status.Reason, "Connection refused");
    }

    [TestMethod]
    public async Task Timeout_IsUnavailable()
    {
        var service = WhatsAppServiceTestKit.Create(StubHandler.Throws(new TaskCanceledException("timeout")));

        var status = await service.GetConnectionStateAsync();

        Assert.AreEqual(WhatsAppConnectionState.Unavailable, status.State);
    }

    [TestMethod]
    public async Task UnexpectedBody_IsUnavailable()
    {
        var service = WhatsAppServiceTestKit.Create(StubHandler.Json(HttpStatusCode.OK, "não é json"));

        var status = await service.GetConnectionStateAsync();

        Assert.AreEqual(WhatsAppConnectionState.Unavailable, status.State);
    }

    [TestMethod]
    public async Task WithoutBaseUrl_IsNotConfigured_AndDoesNotCall()
    {
        var handler = StubHandler.Json(HttpStatusCode.OK, "{}");
        var service = WhatsAppServiceTestKit.Create(handler, baseUrl: "");

        var status = await service.GetConnectionStateAsync();

        Assert.AreEqual(WhatsAppConnectionState.NotConfigured, status.State);
        Assert.AreEqual(0, handler.Calls.Count);
    }
}
