using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TVS_App.Infrastructure.Data;
using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class MensagemDeTesteTests : VerificationTestBase
{
    private static async Task<(int Customers, int ServiceOrders)> CountAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDataContext>();
        return (await db.Customers.CountAsync(), await db.ServiceOrders.CountAsync());
    }

    [TestMethod("Envio de teste manda mensagem marcada como teste para o número configurado, sem criar dados")]
    public async Task Envio_de_teste()
    {
        var antes = await CountAsync();

        var result = await Api.PostAsync("/internal/whatsapp/test-message");

        Check.Status("Envio de teste", HttpStatusCode.OK, result);
        StringAssert.Contains(result.Message, "Mensagem de teste enviada para 41*****9999");
        await ExpectWhatsAppAsync("Envio de teste", ApiFactory.TestPhone, "Mensagem de teste", "TVS Eletrônica");
        Check.Equal("Envio de teste não cria dados", antes, await CountAsync());
    }

    [TestMethod("Envio de teste com a Evolution falhando devolve o motivo")]
    public async Task Envio_de_teste_com_falha()
    {
        WhatsApp.ForcedStatus = HttpStatusCode.InternalServerError;

        var result = await Api.PostAsync("/internal/whatsapp/test-message");

        Check.Status("Envio de teste com falha", HttpStatusCode.BadGateway, result);
        StringAssert.StartsWith(result.Message, "Falha ao enviar a mensagem de teste:");
    }

    [TestMethod("Monitor de conexão fica desligado durante a verificação")]
    public async Task Monitor_desligado()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        Check.True("Monitor desligado", WhatsApp.OtherRequests.All(r => !r.Contains("connectionState")),
            "nenhuma consulta de conexão", string.Join(", ", WhatsApp.OtherRequests));
    }
}
