using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Kasta.Data.Models;
using Kasta.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NpgsqlTypes;

namespace Kasta.Data;

public class SqliteDbContext : KastaDbContext
{
    public SqliteDbContext(DbContextOptions<SqliteDbContext> options)
        : base(options)
    {
        Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }

    /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var c = new SqliteDatabaseConfig();
        try
        {
            c = KastaConfig.Instance.SqliteDatabase;
        }
        catch (Exception e)
        {
            Trace.WriteLine("Failed to get SqliteDatabase from KastaConfig: " + e.ToString());
        }
        optionsBuilder.UseSqlite(c.ToConnectionString());
        base.OnConfiguring(optionsBuilder);
    }*/

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Ignore<NpgsqlTsVector>();
        base.OnModelCreating(builder);
        builder.Ignore<NpgsqlTsVector>();
        builder.Entity<FileModel>()
            .Ignore(e => e.SearchVector);
    }
}

public class SqliteWalInterceptor : IDbConnectionInterceptor
{
    DbConnection ConnectionCreated(ConnectionCreatedEventData eventData, DbConnection result)
    {
        var cmd = result.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode = WAL;";
        cmd.CommandType = CommandType.Text;
        cmd.ExecuteNonQuery();
        return result;
    }
}