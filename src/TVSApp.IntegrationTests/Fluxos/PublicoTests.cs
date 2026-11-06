using System.Net.Http.Json;
using System.Net;
using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class PublicoTests : VerificationTestBase
{
    private async Task<(long Id, string Code, (long Id, string Name, string Phone) Customer)> OsComOrcamentoAsync()
    {
        var customer = await Api.CreateCustomerAsync();
        var id = await Api.CreateServiceOrderAsync(customer.Id);
        await Api.AddEstimateAsync(id);
        var link = await ExpectWhatsAppAsync("Orçamento (link)", customer.Phone, $"id={id}", "code=");
        var code = link.Text.Split("code=").Last().Trim();
        WhatsApp.Clear();
        return (id, code, customer);
    }

    [TestMethod("Cliente consulta e aprova o orçamento pelo link com o código")]
    public async Task Cliente_aprova_pelo_link()
    {
        var (id, code, customer) = await OsComOrcamentoAsync();
        var anonymous = Factory.CreateClient();

        var consulta = await anonymous.GetAsync($"/get-service-order-for-customer/{id}/{code.ToLowerInvariant()}");
        Check.Equal("Consulta pública", HttpStatusCode.OK, consulta.StatusCode);

        var aprovacao = await anonymous.PutAsJsonAsync("/add-service-order-approve-estimate", new { id, code });
        Check.Equal("Aprovação pelo cliente", HttpStatusCode.OK, aprovacao.StatusCode);

        await ExpectWhatsAppAsync("Agradecimento ao cliente", customer.Phone, $"OS #{id}", "aprovada");
        await ExpectWhatsAppAsync("Aviso ao dono", ApiFactory.OwnerNumber, $"#{id}", "APROVOU");
    }

    [TestMethod("Código de segurança errado é recusado sem enviar mensagem")]
    public async Task Codigo_errado()
    {
        var (id, _, customer) = await OsComOrcamentoAsync();
        var anonymous = Factory.CreateClient();

        var consulta = await anonymous.GetAsync($"/get-service-order-for-customer/{id}/CODIGOERRADO");
        Check.Equal("Consulta com código errado", HttpStatusCode.NotFound, consulta.StatusCode);

        var aprovacao = await anonymous.PutAsJsonAsync("/add-service-order-approve-estimate", new { id, code = "CODIGOERRADO" });
        Check.True("Aprovação com código errado", !aprovacao.IsSuccessStatusCode, "recusa", ((int)aprovacao.StatusCode).ToString());

        await ExpectNoWhatsAppAsync("Código errado (cliente)", customer.Phone);
        await ExpectNoWhatsAppAsync("Código errado (dono)", ApiFactory.OwnerNumber);
    }

    [TestMethod("Contato do site é encaminhado ao WhatsApp do dono")]
    public async Task Contato_do_site()
    {
        var anonymous = Factory.CreateClient();
        var visitante = ApiClient.UniqueName("VISITANTE");
        using var form = new MultipartFormDataContent
        {
            { new StringContent(visitante), "name" },
            { new StringContent("41988887777"), "phone" },
            { new StringContent("Orçamento"), "subject" },
            { new StringContent("Minha TV não liga"), "message" }
        };

        var response = await anonymous.PostAsync("/v1/whatsapp/site-contact", form);

        Check.Equal("Contato do site", HttpStatusCode.OK, response.StatusCode);
        await ExpectWhatsAppAsync("Contato do site (dono)", ApiFactory.OwnerNumber, visitante, "5541988887777");
    }

    [TestMethod("Contato do site sem nome e telefone é recusado")]
    public async Task Contato_do_site_invalido()
    {
        var anonymous = Factory.CreateClient();
        using var form = new MultipartFormDataContent { { new StringContent("sem dados"), "message" } };

        var response = await anonymous.PostAsync("/v1/whatsapp/site-contact", form);

        Check.Equal("Contato do site inválido", HttpStatusCode.BadRequest, response.StatusCode);
    }
}
