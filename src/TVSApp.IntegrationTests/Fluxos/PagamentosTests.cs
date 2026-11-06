using System.Net;
using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class PagamentosTests : VerificationTestBase
{
    [TestMethod("Pagamentos: adicionar, alterar, listar, comprovante e remover")]
    public async Task Pagamentos()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);

        Check.Status("Adicionar pagamento", HttpStatusCode.OK,
            await Api.PostAsync($"/service-orders/{id}/payments", new { amount = 150m, paymentDate = "2026-11-06T10:00:00" }));

        var lista = await Api.GetAsync($"/service-orders/{id}/payments");
        Check.Status("Listar pagamentos", HttpStatusCode.OK, lista);
        var pagamentos = lista.Data!.AsArray();
        Check.Equal("Listar pagamentos (quantidade)", 1, pagamentos.Count);
        var paymentId = pagamentos[0]!["id"]!.GetValue<long>();

        Check.Status("Alterar pagamento", HttpStatusCode.OK,
            await Api.PutAsync($"/service-orders/{id}/payments/{paymentId}", new { amount = 200m, paymentDate = "2026-11-06T11:00:00" }));
        var alterado = await Api.GetAsync($"/service-orders/{id}/payments");
        Check.Equal("Valor alterado", 200m, alterado.Data![0]!["amount"]!.GetValue<decimal>());

        var comprovante = await Api.GetAsync($"/service-orders/{id}/payments/{paymentId}/receipt");
        Check.Equal("Comprovante", HttpStatusCode.OK, comprovante.Status);
        Check.Equal("Comprovante (PDF)", "application/pdf", comprovante.Response.Content.Headers.ContentType?.MediaType);

        Check.Status("Remover pagamento", HttpStatusCode.OK, await Api.DeleteAsync($"/service-orders/{id}/payments/{paymentId}"));
        var vazio = await Api.GetAsync($"/service-orders/{id}/payments");
        Check.Equal("Pagamentos depois de remover", 0, vazio.Data!.AsArray().Count);
    }

    [TestMethod("Pagamento com valor zero é recusado")]
    public async Task Pagamento_invalido()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);

        var result = await Api.PostAsync($"/service-orders/{id}/payments", new { amount = 0m, paymentDate = "2026-11-06T10:00:00" });

        Check.Status("Pagamento zero", HttpStatusCode.BadRequest, result);
        Check.Equal("Pagamento zero (mensagem)", "O valor deve ser maior que zero", result.Message);
    }
}
