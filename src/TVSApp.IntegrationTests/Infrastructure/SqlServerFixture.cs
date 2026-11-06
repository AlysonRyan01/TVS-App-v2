using Testcontainers.MsSql;

namespace TVS_App.IntegrationTests.Infrastructure;

/// <summary>SQL Server descartável (container) usado por toda a execução da verificação.</summary>
[TestClass]
public static class SqlServerFixture
{
    private static MsSqlContainer? _container;

    public static string ConnectionString { get; private set; } = string.Empty;

    [AssemblyInitialize]
    public static async Task StartAsync(TestContext _)
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString()
            .Replace("Database=master", "Database=TvsVerificacao", StringComparison.OrdinalIgnoreCase);
    }

    [AssemblyCleanup]
    public static async Task StopAsync()
    {
        ApiFactory.DisposeShared();
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
