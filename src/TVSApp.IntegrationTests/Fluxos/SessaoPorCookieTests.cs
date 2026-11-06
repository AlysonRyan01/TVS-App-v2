using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class SessaoPorCookieTests : VerificationTestBase
{
    private const string CookieName = "tvs_refresh";

    private static async Task<HttpResponseMessage> LoginRawAsync(HttpClient client) =>
        await client.PostAsJsonAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });

    private static string? SessionSetCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith(CookieName + "=", StringComparison.Ordinal))
            : null;

    private static string CookieValue(string setCookie) => setCookie.Split(';')[0][(CookieName.Length + 1)..];

    private static HttpRequestMessage WithCookie(HttpMethod method, string url, string value)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Cookie", $"{CookieName}={value}");
        return request;
    }

    private static async Task<JsonNode?> JsonAsync(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync());

    private static bool IsDeletion(string setCookie) =>
        setCookie.StartsWith(CookieName + "=;", StringComparison.Ordinal) || setCookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);

    // ---------- US1: continuar logado ----------

    [TestMethod("Login grava o cookie de sessão e devolve o token de acesso")]
    public async Task Login_grava_cookie()
    {
        var response = await LoginRawAsync(Factory.CreateRawClient());

        Check.Equal("Login", HttpStatusCode.OK, response.StatusCode);
        var setCookie = SessionSetCookie(response);
        Check.True("Cookie de sessão", setCookie is not null && !IsDeletion(setCookie), $"Set-Cookie {CookieName}", setCookie ?? "ausente");
        var body = await JsonAsync(response);
        Check.True("Token de acesso", !string.IsNullOrWhiteSpace(body?["data"]?["accessToken"]?.GetValue<string>()), "accessToken no corpo", "vazio");
    }

    [TestMethod("Refresh com o cookie devolve novo acesso, sem corpo")]
    public async Task Refresh_com_cookie()
    {
        var client = Factory.CreateClient(); // guarda o cookie como o navegador
        Check.Equal("Login", HttpStatusCode.OK, (await LoginRawAsync(client)).StatusCode);

        var refresh = await client.PostAsync("/auth/refresh", null);

        Check.Equal("Refresh", HttpStatusCode.OK, refresh.StatusCode);
        var token = (await JsonAsync(refresh))?["data"]?.GetValue<string>();
        Check.True("Refresh", !string.IsNullOrWhiteSpace(token), "novo token de acesso", "vazio");

        var request = new HttpRequestMessage(HttpMethod.Get, "/service-orders/pending-estimate");
        request.Headers.Authorization = new("Bearer", token);
        Check.Equal("Usar o token renovado", HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);
    }

    [TestMethod("Refresh sem cookie é recusado")]
    public async Task Refresh_sem_cookie()
    {
        var refresh = await Factory.CreateRawClient().PostAsync("/auth/refresh", null);

        Check.Equal("Refresh sem cookie", HttpStatusCode.Unauthorized, refresh.StatusCode);
        var body = await JsonAsync(refresh);
        Check.Equal("Refresh sem cookie (code)", 401, body?["code"]?.GetValue<int>());
        Check.Equal("Refresh sem cookie (mensagem)", "Sessão expirada. Entre novamente.", body?["message"]?.GetValue<string>());
    }

    // ---------- US2: sessão protegida ----------

    [TestMethod("Cookie de sessão é HttpOnly, Secure, SameSite=Strict, Path=/ e expira em ~30 dias")]
    public async Task Atributos_do_cookie()
    {
        var setCookie = SessionSetCookie(await LoginRawAsync(Factory.CreateRawClient()))!;
        var parts = setCookie.Split(';').Select(p => p.Trim()).ToList();
        bool Has(string attr) => parts.Any(p => p.Equals(attr, StringComparison.OrdinalIgnoreCase));

        Check.True("HttpOnly", Has("httponly"), "httponly", setCookie);
        Check.True("Secure", Has("secure"), "secure", setCookie);
        Check.True("SameSite", Has("samesite=strict"), "samesite=strict", setCookie);
        Check.True("Path", Has("path=/"), "path=/", setCookie);

        var expiresText = parts.First(p => p.StartsWith("expires=", StringComparison.OrdinalIgnoreCase))["expires=".Length..];
        var days = (DateTimeOffset.Parse(expiresText) - DateTimeOffset.UtcNow).TotalDays;
        Check.True("Expiração", days is > 29 and < 31, "entre 29 e 31 dias", $"{days:F1} dias");
    }

    [TestMethod("Nenhuma resposta de autenticação expõe o refresh token")]
    public async Task Corpo_sem_refresh_token()
    {
        var client = Factory.CreateRawClient();
        var login = await LoginRawAsync(client);
        var cookieValue = CookieValue(SessionSetCookie(login)!);
        var loginBody = await login.Content.ReadAsStringAsync();

        var refresh = await client.SendAsync(WithCookie(HttpMethod.Post, "/auth/refresh", cookieValue));
        var refreshBody = await refresh.Content.ReadAsStringAsync();

        foreach (var (etapa, body) in new[] { ("Login", loginBody), ("Refresh", refreshBody) })
        {
            Check.True(etapa, !body.Contains("refreshToken", StringComparison.OrdinalIgnoreCase), "sem refreshToken", body);
            Check.True(etapa, !body.Contains(cookieValue), "sem o valor da sessão", "valor do cookie no corpo");
        }
    }

    [TestMethod("Cadastro de usuário não devolve tokens nem troca a sessão de quem está logado")]
    public async Task Register_nao_troca_sessao()
    {
        var client = Factory.CreateClient();
        var login = await LoginRawAsync(client);
        var token = (await JsonAsync(login))?["data"]?["accessToken"]?.GetValue<string>();

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/register")
        {
            Content = JsonContent.Create(new { email = $"novo{Guid.NewGuid():N}@tvs.local", password = "Novo@12345" })
        };
        request.Headers.Authorization = new("Bearer", token);
        var register = await client.SendAsync(request);

        Check.Equal("Cadastro", HttpStatusCode.OK, register.StatusCode);
        Check.True("Cadastro (corpo)", (await JsonAsync(register))?["data"] is null, "data nulo", "tokens no corpo");
        Check.True("Cadastro (cookie)", SessionSetCookie(register) is null, "sem Set-Cookie", SessionSetCookie(register) ?? "");
        Check.Equal("Sessão original continua", HttpStatusCode.OK, (await client.PostAsync("/auth/refresh", null)).StatusCode);
    }

    // ---------- US3: sair ----------

    [TestMethod("Sair sem token de acesso revoga a sessão e apaga o cookie")]
    public async Task Logout_sem_bearer()
    {
        var client = Factory.CreateRawClient();
        var cookieValue = CookieValue(SessionSetCookie(await LoginRawAsync(client))!);

        var logout = await client.SendAsync(WithCookie(HttpMethod.Post, "/auth/logout", cookieValue));

        Check.Equal("Sair", HttpStatusCode.OK, logout.StatusCode);
        var setCookie = SessionSetCookie(logout);
        Check.True("Sair (cookie apagado)", setCookie is not null && IsDeletion(setCookie), "Set-Cookie de remoção", setCookie ?? "ausente");

        var refresh = await client.SendAsync(WithCookie(HttpMethod.Post, "/auth/refresh", cookieValue));
        Check.Equal("Refresh depois de sair", HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [TestMethod("Sessão revogada é recusada e o cookie é apagado")]
    public async Task Sessao_revogada()
    {
        var client = Factory.CreateRawClient();
        var cookieValue = CookieValue(SessionSetCookie(await LoginRawAsync(client))!);
        await client.SendAsync(WithCookie(HttpMethod.Post, "/auth/logout", cookieValue));

        var outroNavegador = Factory.CreateRawClient();
        var refresh = await outroNavegador.SendAsync(WithCookie(HttpMethod.Post, "/auth/refresh", cookieValue));

        Check.Equal("Refresh com sessão revogada", HttpStatusCode.Unauthorized, refresh.StatusCode);
        var setCookie = SessionSetCookie(refresh);
        Check.True("Cookie apagado", setCookie is not null && IsDeletion(setCookie), "Set-Cookie de remoção", setCookie ?? "ausente");
    }

    [TestMethod("Sair sem sessão também responde OK")]
    public async Task Logout_sem_sessao()
    {
        var logout = await Factory.CreateRawClient().PostAsync("/auth/logout", null);

        Check.Equal("Sair sem sessão", HttpStatusCode.OK, logout.StatusCode);
    }
}
