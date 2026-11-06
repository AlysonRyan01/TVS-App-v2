using System.Net;
using System.Net.Http.Json;
using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class VendaTests : VerificationTestBase
{
    [TestMethod("Venda gera PDF e envia WhatsApp de venda")]
    public async Task Venda()
    {
        var customer = await Api.CreateCustomerAsync();

        var response = await Api.Http.PostAsJsonAsync("/service-orders/sales", new
        {
            customerId = customer.Id, productBrand = "SAMSUNG", productModel = "UN50", productSerialNumber = "SN-VENDA",
            guarantee = "90 DIAS", amount = 1800m, productType = 1
        });

        Check.Equal("Venda", HttpStatusCode.OK, response.StatusCode);
        Check.Equal("Venda (PDF)", "application/pdf", response.Content.Headers.ContentType?.MediaType);
        Check.True("Venda (X-Service-Order-Id)", response.Headers.Contains("X-Service-Order-Id"), "cabeçalho", "ausente");
        await ExpectWhatsAppAsync("WhatsApp de venda", customer.Phone, "Agradecemos por adquirir", "90 DIAS");
    }
}
