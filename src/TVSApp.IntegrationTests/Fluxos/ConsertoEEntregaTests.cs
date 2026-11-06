using System.Net;
using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class ConsertoEEntregaTests : VerificationTestBase
{
    private async Task<((long Id, string Name, string Phone) Customer, long Id)> OsAprovadaAsync()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);
        await Api.AddEstimateAsync(id);
        Check.Status("Aprovar orçamento", HttpStatusCode.OK, await Api.PutAsync($"/service-orders/{id}/estimate/approve"));
        return (customer, id);
    }

    [TestMethod("Compra de peça e conserto enviam aviso de aparelho pronto")]
    public async Task Conserto()
    {
        var (customer, id) = await OsAprovadaAsync();

        Check.Status("Compra de peça", HttpStatusCode.OK, await Api.PutAsync($"/service-orders/{id}/part/purchase"));
        Check.Status("Custo da peça", HttpStatusCode.OK, await Api.PutAsync($"/service-orders/{id}/part/purchase-cost", new { partPurchaseCost = 130m }));
        Check.Status("Conserto", HttpStatusCode.OK, await Api.PutAsync($"/service-orders/{id}/repair"));

        await ExpectWhatsAppAsync("Aparelho pronto", customer.Phone, $"nº {id}", "pronta para retirada");
    }

    [TestMethod("Conserto sem orçamento aprovado é recusado")]
    public async Task Conserto_sem_aprovacao()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);

        var result = await Api.PutAsync($"/service-orders/{id}/repair");

        Check.Status("Conserto sem aprovação", HttpStatusCode.BadRequest, result);
    }

    [TestMethod("Entrega de OS aprovada com conserto devolve PDF e envia garantia")]
    public async Task Entrega()
    {
        var (customer, id) = await OsAprovadaAsync();
        Check.Status("Conserto", HttpStatusCode.OK, await Api.PutAsync($"/service-orders/{id}/repair"));

        var result = await Api.PutAsync($"/service-orders/{id}/delivery");

        Check.Status("Entrega", HttpStatusCode.OK, result);
        Check.Equal("Entrega (PDF)", "application/pdf", result.Response.Content.Headers.ContentType?.MediaType);
        await ExpectWhatsAppAsync("Garantia", customer.Phone, $"nº {id}", "Garantia");

        var os = await Api.GetAsync($"/service-orders/{id}");
        Check.Equal("Situação depois da entrega", 4, os.Data?["serviceOrderStatus"]?.GetValue<int>());
    }
}
