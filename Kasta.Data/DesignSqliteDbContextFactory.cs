using Kasta.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Kasta.Data;

public class DesignSqliteDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>
{
    SqliteDbContext IDesignTimeDbContextFactory<SqliteDbContext>.CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddDefaultIdentity<UserModel>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<KastaDbContext>();
        var serviceProvider = services.BuildServiceProvider();

        var builder = new DbContextOptionsBuilder<SqliteDbContext>();
        builder.UseApplicationServiceProvider(serviceProvider);
        builder.UseSqlite("DataSource=kasta-design.db");
        return new SqliteDbContext(builder.Options);
    }
}