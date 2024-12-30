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
        var cfg = KastaConfig.Get();
        var b = new NpgsqlConnectionStringBuilder();
        b.Host = cfg.Database.Hostname;
        b.Port = cfg.Database.Port;
        b.Username = cfg.Database.Username;
        b.Password = cfg.Database.Password;
        b.Database = cfg.Database.Name;
        builder.UseNpgsql(b.ToString());

        return new ApplicationDbContext(builder.Options);
    }
}