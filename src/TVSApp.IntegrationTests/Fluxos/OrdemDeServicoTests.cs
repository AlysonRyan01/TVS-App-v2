using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class OrdemDeServicoTests : VerificationTestBase
{
    [TestMethod("Abrir OS gera PDF, número e WhatsApp de boas-vindas")]
    public async Task Abrir_os()
    {
        var customer = await Api.CreateCustomerAsync();

        var id = await Api.CreateServiceOrderAsync(customer.Id);

        await ExpectWhatsAppAsync("Boas-vindas", customer.Phone, "bem-vindo", $"nº {id}");
    }
}
