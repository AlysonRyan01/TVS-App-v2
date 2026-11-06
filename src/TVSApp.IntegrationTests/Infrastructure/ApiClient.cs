using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TVS_App.IntegrationTests.Infrastructure;

public sealed record ApiResult(HttpStatusCode Status, JsonNode? Body, HttpResponseMessage Response)
{
    public JsonNode? Data => Body?["data"];
    public string? Message => Body?["message"]?.GetValue<string>();
    public int? Code => Body?["code"]?.GetValue<int>();
}

/// <summary>Cliente HTTP da verificação: fala com a API como o Angular faz (JSON camelCase, Bearer).</summary>
public sealed class ApiClient
{
    private static int _sequence;
    private readonly HttpClient _http;

    public ApiClient(ApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    public HttpClient Http => _http;

    public static string NextPhone() => $"4190000{Interlocked.Increment(ref _sequence):D4}";

    public static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    public async Task LoginAsync()
    {
        var result = await PostAsync("/auth/login", new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        Check.Status("Login", HttpStatusCode.OK, result);
        var token = result.Data?["accessToken"]?.GetValue<string>();
        Check.True("Login", !string.IsNullOrWhiteSpace(token), "accessToken preenchido", "vazio");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<(long Id, string Name, string Phone)> CreateCustomerAsync(string? name = null)
    {
        name ??= UniqueName("CLIENTE VERIFICACAO");
        var phone = NextPhone();
        var result = await PostAsync("/customers", new
        {
            name, phone, phone2 = "", email = "", document = (string?)null,
            street = "RUA TESTE", number = "1", neighborhood = "CENTRO", city = "CAMPO LARGO", state = "PR", zipCode = "83600000"
        });
        Check.Status("Cadastrar cliente", HttpStatusCode.OK, result);
        return (result.Data!["id"]!.GetValue<long>(), name.ToUpperInvariant(), phone);
    }

    public async Task<long> CreateServiceOrderAsync(long customerId, int enterprise = 1)
    {
        var response = await _http.PostAsJsonAsync("/service-orders", new
        {
            customerId, productBrand = "LG", productModel = "42LB5600", productSerialNumber = "SN-VERIF",
            productDefect = "SEM IMAGEM", accessories = "CONTROLE", productType = 1, enterprise
        });
        Check.Equal("Abrir OS", HttpStatusCode.OK, response.StatusCode);
        Check.Equal("Abrir OS (PDF)", "application/pdf", response.Content.Headers.ContentType?.MediaType);
        Check.True("Abrir OS (X-Service-Order-Id)", response.Headers.TryGetValues("X-Service-Order-Id", out var values),
            "cabeçalho X-Service-Order-Id", "ausente");
        return long.Parse(values!.First());
    }

    public async Task AddEstimateAsync(long id, int repairResult = 1, decimal totalCost = 350m)
    {
        var result = await PutAsync($"/service-orders/{id}/estimate", new
        {
            solution = "TROCA DA PLACA FONTE", estimateMessage = "Orçamento da verificação", repairResult,
            totalCost, warranty = "90 DIAS", partPurchaseCost = 120m
        });
        Check.Status("Orçamento", HttpStatusCode.OK, result);
    }

    public Task<ApiResult> GetAsync(string url) => SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

    public Task<ApiResult> PostAsync(string url, object? body = null) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, url) { Content = body is null ? null : JsonContent.Create(body) });

    public Task<ApiResult> PutAsync(string url, object? body = null) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Put, url) { Content = body is null ? null : JsonContent.Create(body) });

    public Task<ApiResult> DeleteAsync(string url) => SendAsync(new HttpRequestMessage(HttpMethod.Delete, url));

    public async Task<ApiResult> SendAsync(HttpRequestMessage request)
    {
        var response = await _http.SendAsync(request);
        JsonNode? body = null;
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            var text = await response.Content.ReadAsStringAsync();
            try { body = string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text); }
            catch (JsonException) { body = null; }
        }
        return new ApiResult(response.StatusCode, body, response);
    }
}

/// <summary>Asserts com mensagem em português no formato "Etapa X: esperado … / obtido …".</summary>
public static class Check
{
    public static void Equal<T>(string etapa, T esperado, T obtido) =>
        Assert.AreEqual(esperado, obtido, $"Etapa {etapa}: esperado {esperado} / obtido {obtido}");

    public static void True(string etapa, bool condicao, string esperado, string obtido) =>
        Assert.IsTrue(condicao, $"Etapa {etapa}: esperado {esperado} / obtido {obtido}");

    public static void Status(string etapa, HttpStatusCode esperado, ApiResult result) =>
        Assert.AreEqual(esperado, result.Status,
            $"Etapa {etapa}: esperado HTTP {(int)esperado} / obtido HTTP {(int)result.Status} — {result.Message}");
}
