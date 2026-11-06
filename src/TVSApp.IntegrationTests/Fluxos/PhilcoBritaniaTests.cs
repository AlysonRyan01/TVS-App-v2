using System.Net;
using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class PhilcoBritaniaTests : VerificationTestBase
{
    private const int PhilcoBritania = 16;

    [TestMethod("Philco/Britania: orçamento com conserto já fica aprovado e não envia WhatsApp")]
    public async Task Sem_whatsapp_de_orcamento()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id, PhilcoBritania);
        await ExpectWhatsAppAsync("Boas-vindas", customer.Phone, $"nº {id}");
        WhatsApp.Clear();

        await Api.AddEstimateAsync(id);

        await ExpectNoWhatsAppAsync("Orçamento Philco/Britania (cliente)", customer.Phone);
        await ExpectNoWhatsAppAsync("Orçamento Philco/Britania (dono)", ApiFactory.OwnerNumber);
        var os = await Api.GetAsync($"/service-orders/{id}");
        Check.Equal("Orçamento Philco/Britania aprovado automaticamente (repairStatus)", 2, os.Data?["repairStatus"]?.GetValue<int>());

        var aprovar = await Api.PutAsync($"/service-orders/{id}/estimate/approve");
        Check.Status("Aprovar de novo", HttpStatusCode.Conflict, aprovar);
    }
}
