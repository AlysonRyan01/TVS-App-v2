using Microsoft.EntityFrameworkCore;
using TVS_App.Infrastructure.Data;

namespace TVS_App.MigrarDadosAccess;

public class Program
{
    public static async Task Main(string[] args)
    {
        string accessConnStr = @"Provider=Microsoft.ACE.OLEDB.16.0;Data Source=C:\sisos\os.mdb;";

        var options = new DbContextOptionsBuilder<ApplicationDataContext>()
            .UseSqlServer("")
            .Options;

        var dbContext = new ApplicationDataContext(options);

        var migrador = new AccessToSqlMigrator(accessConnStr, dbContext);
        await migrador.MigrarClientesAsync();
        await migrador.MigrarOrdensDeServicoAsync();

        Console.WriteLine("Migração concluída com sucesso!");
    }
}