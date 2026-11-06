using System.Net;
using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class ConsultasTests : VerificationTestBase
{
    [TestMethod("Busca encontra a OS pelo nome do cliente")]
    public async Task Busca()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);

        var result = await Api.GetAsync($"/service-orders/search?customer={Uri.EscapeDataString(customer.Name)}");

        Check.Status("Busca", HttpStatusCode.OK, result);
        var numeros = result.Data!["items"]!.AsArray().Select(i => i!["number"]!.GetValue<long>()).ToList();
        Check.True("Busca", numeros.Contains(id), $"OS {id} na lista", string.Join(", ", numeros));
    }

    [TestMethod("Listas por etapa e detalhe da OS respondem")]
    public async Task Listas_e_detalhe()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);

        var pendentes = await Api.GetAsync("/service-orders/pending-estimate?pageSize=100");
        Check.Status("Orçamentos pendentes", HttpStatusCode.OK, pendentes);
        foreach (var etapa in new[] { "awaiting-response", "approved", "rejected", "awaiting-part", "ready-for-pickup" })
            Check.Status($"Lista {etapa}", HttpStatusCode.OK, await Api.GetAsync($"/service-orders/{etapa}"));

        var detalhe = await Api.GetAsync($"/service-orders/{id}");
        Check.Status("Detalhe da OS", HttpStatusCode.OK, detalhe);
        Check.Equal("Detalhe da OS (cliente)", customer.Name, detalhe.Data?["name"]?.GetValue<string>());

        var pdf = await Api.GetAsync($"/service-orders/{id}/pdf");
        Check.Equal("Reimprimir PDF", "application/pdf", pdf.Response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod("Métricas do dashboard respondem")]
    public async Task Metricas()
    {
        var result = await Api.GetAsync("/metrics");

        Check.Status("Métricas", HttpStatusCode.OK, result);
        Check.True("Métricas", result.Data?["totalOrders"] is not null, "totalOrders", "ausente");
    }
}
