using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TVS_App.Infrastructure.Data;

public class ApplicationDataContextFactory : IDesignTimeDbContextFactory<ApplicationDataContext>
{
    public ApplicationDataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDataContext>();
            optionsBuilder.UseSqlServer(
                "Server=tvs-server.database.windows.net;Database=tvs-database;User Id=tvsuser;Password=Pu123456;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");

            return new ApplicationDataContext(optionsBuilder.Options);
    }
}