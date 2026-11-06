using System.Net;
using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class OrcamentoTests : VerificationTestBase
{
    [TestMethod("Orçamento envia WhatsApp com número da OS e link com código de segurança")]
    public async Task Orcamento()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);

        await Api.AddEstimateAsync(id);

        await ExpectWhatsAppAsync("Orçamento (texto)", customer.Phone, $"Ordem de Serviço nº {id}", "aprovar ou recusar");
        var link = await ExpectWhatsAppAsync("Orçamento (link)", customer.Phone, "tvseletronica.com/consultar", $"id={id}", "code=");
        Check.True("Orçamento (código)", link.Text.Split("code=").Last().Trim().Length > 0, "código de segurança no link", link.Text);
    }

    [TestMethod("Aprovação pela loja envia agradecimento ao cliente e aviso ao dono")]
    public async Task Aprovacao_pela_loja()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);
        await Api.AddEstimateAsync(id, totalCost: 350m);

        var result = await Api.PutAsync($"/service-orders/{id}/estimate/approve");

        Check.Status("Aprovar orçamento", HttpStatusCode.OK, result);
        await ExpectWhatsAppAsync("Agradecimento ao cliente", customer.Phone, $"OS #{id}", "aprovada", "350");
        await ExpectWhatsAppAsync("Aviso ao dono", ApiFactory.OwnerNumber, $"#{id}", "APROVOU", customer.Name);
    }

    [TestMethod("Reprovação envia confirmação ao cliente e aviso ao dono")]
    public async Task Reprovacao()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);
        await Api.AddEstimateAsync(id);

        var result = await Api.PutAsync($"/service-orders/{id}/estimate/reject");

        Check.Status("Reprovar orçamento", HttpStatusCode.OK, result);
        await ExpectWhatsAppAsync("Confirmação ao cliente", customer.Phone, $"OS: #{id}", "não aprovado");
        await ExpectWhatsAppAsync("Aviso ao dono", ApiFactory.OwnerNumber, $"#{id}", "REJEITOU");
    }

    [TestMethod("Liberar reprovado envia aviso de retirada")]
    public async Task Liberar_reprovado()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);
        await Api.AddEstimateAsync(id);
        Check.Status("Reprovar orçamento", HttpStatusCode.OK, await Api.PutAsync($"/service-orders/{id}/estimate/reject"));

        var result = await Api.PutAsync($"/service-orders/{id}/ready-for-pickup");

        Check.Status("Liberar reprovado", HttpStatusCode.OK, result);
        await ExpectWhatsAppAsync("Aviso de retirada", customer.Phone, "Pronto para retirada", "não foi aprovado");
    }

    [TestMethod("Responder um orçamento já respondido é recusado")]
    public async Task Orcamento_ja_respondido()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);
        await Api.AddEstimateAsync(id);
        Check.Status("Aprovar orçamento", HttpStatusCode.OK, await Api.PutAsync($"/service-orders/{id}/estimate/approve"));

        var result = await Api.PutAsync($"/service-orders/{id}/estimate/reject");

        Check.Status("Reprovar depois de aprovado", HttpStatusCode.Conflict, result);
        Check.Equal("Reprovar depois de aprovado (mensagem)", "Esse orçamento já foi respondido.", result.Message);
    }
}
