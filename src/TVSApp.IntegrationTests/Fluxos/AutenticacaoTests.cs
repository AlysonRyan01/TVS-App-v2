using System.Net;
using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class AutenticacaoTests : VerificationTestBase
{
    [TestMethod("Login com usuário inicial devolve tokens")]
    public async Task Login_ok()
    {
        var result = await Api.PostAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });

        Check.Status("Login", HttpStatusCode.OK, result);
        Check.True("Login", !string.IsNullOrWhiteSpace(result.Data?["accessToken"]?.GetValue<string>()), "accessToken", "vazio");
        Check.True("Login", result.Data?["refreshToken"] is null, "sem refreshToken no corpo", "refreshToken presente");
    }

    [TestMethod("Login com senha errada é recusado")]
    public async Task Login_senha_errada()
    {
        var result = await Api.PostAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = "senha-errada" });

        Check.Status("Login com senha errada", HttpStatusCode.BadRequest, result);
        Check.Equal("Login com senha errada (code)", 401, result.Code);
        Check.Equal("Login com senha errada (mensagem)", "Email ou senha inválidos", result.Message);
    }
}
