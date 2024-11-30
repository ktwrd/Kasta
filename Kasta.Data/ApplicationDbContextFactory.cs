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
        var b = new NpgsqlConnectionStringBuilder();
        b.Host = FeatureFlags.DatabaseHost;
        b.Port = FeatureFlags.DatabasePort;
        b.Username = FeatureFlags.DatabaseUser;
        b.Password = FeatureFlags.DatabasePassword;
        b.Database = FeatureFlags.DatabaseName;
        builder.UseNpgsql(b.ToString());

        return new ApplicationDbContext(builder.Options);
    }
}