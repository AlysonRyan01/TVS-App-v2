using System.Net;
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

    private static object EditBody(object? estimate) => new
    {
        brand = "LG", model = "42LB5600", serialNumber = "SN-VERIF", defect = "SEM IMAGEM", accessories = "CONTROLE",
        type = 1, location = "B3", enterprise = 1, entryDate = "2026-11-01T00:00:00", inspectionDate = (string?)null,
        responseDate = (string?)null, repairDate = (string?)null, purchasePartDate = (string?)null, deliveryDate = (string?)null,
        solution = "TROCA DA FONTE", guarantee = "90 DIAS", partCost = 100m, laborCost = 200m, estimate
    };

    [TestMethod("Alterar OS muda situação, resposta, resultado, mensagem e custo de compra da peça")]
    public async Task Alterar_orcamento()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);
        WhatsApp.Clear();

        var result = await Api.PutAsync($"/service-orders/{id}", EditBody(new
        {
            serviceOrderStatus = 3, repairStatus = 2, repairResult = 1, estimateMessage = "Corrigido pelo balcão", partPurchaseCost = 75m
        }));

        Check.Status("Alterar OS", HttpStatusCode.OK, result);
        var os = (await Api.GetAsync($"/service-orders/{id}")).Data!;
        Check.Equal("Situação", 3, os["serviceOrderStatus"]!.GetValue<int>());
        Check.Equal("Resposta", 2, os["repairStatus"]!.GetValue<int>());
        Check.Equal("Resultado", 1, os["repairResult"]!.GetValue<int>());
        Check.Equal("Mensagem", "Corrigido pelo balcão", os["estimateMessage"]!.GetValue<string>());
        Check.Equal("Custo de compra da peça", 75m, os["partPurchaseCost"]!.GetValue<decimal>());
        await ExpectNoWhatsAppAsync("Alterar OS não envia WhatsApp", customer.Phone);

        // Sem o bloco "estimate", os dados do orçamento ficam como estão.
        Check.Status("Alterar OS sem orçamento", HttpStatusCode.OK, await Api.PutAsync($"/service-orders/{id}", EditBody(null)));
        os = (await Api.GetAsync($"/service-orders/{id}")).Data!;
        Check.Equal("Situação mantida", 3, os["serviceOrderStatus"]!.GetValue<int>());

        // Resultado pode voltar a vazio.
        await Api.PutAsync($"/service-orders/{id}", EditBody(new
        {
            serviceOrderStatus = 1, repairStatus = 1, repairResult = (int?)null, estimateMessage = (string?)null, partPurchaseCost = 0m
        }));
        os = (await Api.GetAsync($"/service-orders/{id}")).Data!;
        Check.True("Resultado vazio", os["repairResult"] is null, "null", os["repairResult"]?.ToJsonString() ?? "null");
    }
}
