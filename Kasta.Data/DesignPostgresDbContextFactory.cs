using Kasta.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kasta.Data;

public class DesignPostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresDbContext>
{
    PostgresDbContext IDesignTimeDbContextFactory<PostgresDbContext>.CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<PostgresDbContext>();
        var connectionString = KastaConfig.Instance.Database.ToConnectionString();
        if (!connectionString.Trim().EndsWith(';')) connectionString += ';';
        connectionString += "Include Error Detail=true";
        builder.UseNpgsql(connectionString);

        return new PostgresDbContext(builder.Options);
    }
}