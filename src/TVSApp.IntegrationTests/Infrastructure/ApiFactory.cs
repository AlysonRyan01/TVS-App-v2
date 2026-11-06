using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Infrastructure.Services.Whatsapp;

namespace TVS_App.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe a API real (ambiente Production) contra o SQL Server descartável, com a Evolution substituída
/// pelo <see cref="FakeEvolutionHandler"/>. Proteção extra: a URL da Evolution aponta para um domínio
/// <c>.invalid</c>, então nenhum envio real é possível mesmo que o handler falso não fosse aplicado.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "verificacao@tvs.local";
    public const string AdminPassword = "Verificacao@123";
    public const string TestPhone = "41900009999";
    public const string OwnerNumber = "5541992744920";

    private static ApiFactory? _shared;
    private static readonly object Lock = new();

    public FakeEvolutionHandler Fake { get; } = new();

    public static ApiFactory Shared
    {
        get
        {
            lock (Lock)
                return _shared ??= new ApiFactory();
        }
    }

    public static void DisposeShared()
    {
        lock (Lock)
        {
            _shared?.Dispose();
            _shared = null;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.UseSetting("ConnectionStrings:DefaultConnection", SqlServerFixture.ConnectionString);
        builder.UseSetting("Jwt:SecretKey", "chave-da-verificacao-automatica-com-mais-de-32-caracteres");
        builder.UseSetting("Jwt:Issuer", "tvs-verificacao");
        builder.UseSetting("Jwt:Audience", "tvs-verificacao");
        builder.UseSetting("Cors:Origins", "http://localhost");
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:Password", AdminPassword);
        builder.UseSetting("EvolutionApiSettings:BaseUrl", "http://evolution.invalid");
        builder.UseSetting("EvolutionApiSettings:Instance", "tvs-teste");
        builder.UseSetting("EvolutionApiSettings:ApiKey", "chave-teste");
        builder.UseSetting("WhatsAppTest:Phone", TestPhone);

        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<IWhatsappService, WhatsappService>()
                .ConfigurePrimaryHttpMessageHandler(() => Fake);

            // O monitor de conexão consulta a Evolution periodicamente; na verificação ele fica desligado.
            var monitor = services.FirstOrDefault(d => d.ImplementationType?.Name == "WhatsAppConnectionMonitor");
            if (monitor is not null)
                services.Remove(monitor);
        });
    }
}
