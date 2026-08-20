using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kasta.Data;

public class DesignSqliteDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>
{
    SqliteDbContext IDesignTimeDbContextFactory<SqliteDbContext>.CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SqliteDbContext>();
        builder.UseSqlite("DataSource=kasta-design.db");
        return new SqliteDbContext(builder.Options);
    }
}