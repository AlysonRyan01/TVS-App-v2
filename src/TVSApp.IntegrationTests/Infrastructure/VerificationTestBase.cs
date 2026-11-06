using System.Text.RegularExpressions;

namespace TVS_App.IntegrationTests.Infrastructure;

public abstract class VerificationTestBase
{
    protected static ApiFactory Factory => ApiFactory.Shared;
    protected static FakeEvolutionHandler WhatsApp => Factory.Fake;
    protected ApiClient Api { get; private set; } = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        WhatsApp.Clear();
        Api = new ApiClient(Factory);
        await Api.LoginAsync();
    }

    protected static string Formatted(string phone)
    {
        var digits = Regex.Replace(phone, @"\D", "");
        return digits.StartsWith("55") ? digits : "55" + digits;
    }

    /// <summary>Confere que uma mensagem para o telefone, contendo todos os trechos, foi gerada.</summary>
    protected static async Task<CapturedMessage> ExpectWhatsAppAsync(string etapa, string phone, params string[] trechos)
    {
        var number = Formatted(phone);
        var found = await WhatsApp.WaitForAsync(m => m.Number == number && trechos.All(t => m.Text.Contains(t, StringComparison.OrdinalIgnoreCase)));
        if (found is null)
        {
            var paraNumero = WhatsApp.Messages.Where(m => m.Number == number).Select(m => Resumo(m.Text)).ToList();
            Assert.Fail($"Etapa {etapa}: esperado WhatsApp para {number} contendo [{string.Join(", ", trechos)}] / obtido " +
                        (paraNumero.Count == 0 ? "nenhuma mensagem para esse número" : string.Join(" | ", paraNumero)));
        }
        return found!;
    }

    /// <summary>Confere que nenhuma mensagem foi gerada para o telefone.</summary>
    protected static async Task ExpectNoWhatsAppAsync(string etapa, string phone)
    {
        var number = Formatted(phone);
        var messages = await WhatsApp.CollectAsync(m => m.Number == number);
        Assert.AreEqual(0, messages.Count,
            $"Etapa {etapa}: esperado nenhuma mensagem para {number} / obtido {messages.Count}: {string.Join(" | ", messages.Select(m => Resumo(m.Text)))}");
    }

    private static string Resumo(string text)
    {
        var oneLine = text.Replace('\n', ' ');
        return oneLine.Length > 90 ? oneLine[..90] + "…" : oneLine;
    }
}
