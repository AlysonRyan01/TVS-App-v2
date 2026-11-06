using System.Net;
using TVS_App.IntegrationTests.Infrastructure;

namespace TVS_App.IntegrationTests.Fluxos;

[TestClass]
public class ClientesTests : VerificationTestBase
{
    private static string Token() => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

    private static string DigitsToken() => Random.Shared.Next(10_000_000, 99_999_999).ToString();

    private async Task<long> CreateAsync(string name, string phone, string phone2 = "", string? document = null)
    {
        var result = await Api.PostAsync("/customers", new
        {
            name, phone, phone2, email = "", document,
            street = "RUA", number = "1", neighborhood = "CENTRO", city = "CAMPO LARGO", state = "PR", zipCode = "83600000"
        });
        Check.Status($"Cadastrar {name}", HttpStatusCode.OK, result);
        return result.Data!["id"]!.GetValue<long>();
    }

    private async Task<(List<long> Ids, int Total, ApiResult Result)> SearchAsync(string query)
    {
        var result = await Api.GetAsync($"/customers/search?{query}");
        Check.Status($"Buscar ({query})", HttpStatusCode.OK, result);
        var ids = result.Data!["items"]!.AsArray().Select(i => i!["id"]!.GetValue<long>()).ToList();
        return (ids, result.Data!["totalCount"]!.GetValue<int>(), result);
    }

    [TestMethod("Clientes: sem filtros traz os mais recentes primeiro")]
    public async Task Sem_filtros()
    {
        var antigo = await CreateAsync($"CLIENTE ANTIGO {Token()}", "41900001111");
        var novo = await CreateAsync($"CLIENTE NOVO {Token()}", "41900002222");

        var (ids, total, _) = await SearchAsync("pageSize=100");

        Check.True("Ordem", ids.IndexOf(novo) >= 0 && ids.IndexOf(novo) < ids.IndexOf(antigo), "novo antes do antigo", string.Join(",", ids.Take(5)));
        Check.True("Total", total >= 2, ">= 2", total.ToString());
        Check.Equal("Mais recente no topo", ids.Max(), ids[0]);
    }

    [TestMethod("Clientes: nome sem acento encontra nome com acento")]
    public async Task Nome_sem_acento()
    {
        var token = Token();
        var id = await CreateAsync($"João Conceição {token}", "41900003333");

        var (ids, _, _) = await SearchAsync($"name={Uri.EscapeDataString($"joao conceicao {token}".ToLower())}");

        Check.True("Nome sem acento", ids.Contains(id), $"cliente {id}", string.Join(",", ids));
    }

    [TestMethod("Clientes: telefone formatado encontra número com ou sem pontuação, principal ou secundário")]
    public async Task Telefone_so_digitos()
    {
        var d = DigitsToken();                                 // 8 dígitos únicos
        var semPontuacao = await CreateAsync($"FONE A {Token()}", $"41{d}");
        var comPontuacao = await CreateAsync($"FONE B {Token()}", $"(41) {d[..4]}-{d[4..]}");
        var noSecundario = await CreateAsync($"FONE C {Token()}", "41900004444", $"41 {d[..4]} {d[4..]}");
        var outro = await CreateAsync($"FONE D {Token()}", "41900005555");

        var (ids, total, _) = await SearchAsync($"phone={Uri.EscapeDataString($"(41) {d[..4]}-{d[4..]}")}");

        foreach (var (nome, id) in new[] { ("sem pontuação", semPontuacao), ("com pontuação", comPontuacao), ("no secundário", noSecundario) })
            Check.True($"Telefone {nome}", ids.Contains(id), $"cliente {id}", string.Join(",", ids));
        Check.True("Telefone (outro número)", !ids.Contains(outro), "não incluir", "incluído");
        Check.Equal("Telefone (total)", 3, total);
    }

    [TestMethod("Clientes: CPF com pontuação encontra o cliente")]
    public async Task Cpf()
    {
        var id = await CreateAsync($"DOC {Token()}", "41900006666", document: "529.982.247-25");

        var (ids, _, _) = await SearchAsync($"document={Uri.EscapeDataString("529.982.247-25")}");

        Check.True("CPF", ids.Contains(id), $"cliente {id}", string.Join(",", ids));
    }

    [TestMethod("Clientes: filtros são combinados")]
    public async Task Filtros_combinados()
    {
        var token = Token();
        var d = DigitsToken();
        var certo = await CreateAsync($"COMBO {token}", $"41{d}");
        var nomeCerto = await CreateAsync($"COMBO {token} DOIS", "41900007777");

        var (ids, _, _) = await SearchAsync($"name=combo%20{token}&phone=41{d}");

        Check.True("Combinados", ids.Contains(certo) && !ids.Contains(nomeCerto), $"só {certo}", string.Join(",", ids));
    }

    [TestMethod("Clientes: paginação de 20 com total")]
    public async Task Paginacao()
    {
        var token = Token();
        for (var i = 0; i < 21; i++)
            await CreateAsync($"PAGINA {token} {i:D2}", $"41{DigitsToken()}");

        var (pagina1, total, r1) = await SearchAsync($"name=pagina%20{token}");
        var (pagina2, _, _) = await SearchAsync($"name=pagina%20{token}&pageNumber=2");

        Check.Equal("Total", 21, total);
        Check.Equal("Página 1", 20, pagina1.Count);
        Check.Equal("Página 2", 1, pagina2.Count);
        Check.Equal("Tamanho padrão", 20, r1.Data!["pageSize"]!.GetValue<int>());
        Check.Equal("Mensagem", "21 clientes encontrados.", r1.Message);
    }

    [TestMethod("Clientes: nome curto é recusado")]
    public async Task Nome_curto()
    {
        var result = await Api.GetAsync("/customers/search?name=ab");

        Check.Status("Nome curto", HttpStatusCode.BadRequest, result);
        Check.Equal("Nome curto (mensagem)", "Digite pelo menos 3 letras do nome.", result.Message);
    }

    [TestMethod("Clientes: rota antiga /customers?name= continua igual")]
    public async Task Rota_antiga()
    {
        var token = Token();
        var id = await CreateAsync($"ANTIGA {token}", "41900008888");

        var result = await Api.GetAsync($"/customers?name=antiga%20{token}");

        Check.Status("Rota antiga", HttpStatusCode.OK, result);
        var ids = result.Data!.AsArray().Select(i => i!["id"]!.GetValue<long>()).ToList();
        Check.True("Rota antiga", ids.Contains(id), $"cliente {id}", string.Join(",", ids));
    }
}
