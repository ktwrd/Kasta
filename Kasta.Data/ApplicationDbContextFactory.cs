using Kasta.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Kasta.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    ApplicationDbContext IDesignTimeDbContextFactory<ApplicationDbContext>.CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var cfg = KastaConfig.Instance;
        var connectionString = cfg.Database.ToConnectionString();
        connectionString += ";Include Error Detail=true";
        builder.UseNpgsql(connectionString);

        return new ApplicationDbContext(builder.Options);
    }
}