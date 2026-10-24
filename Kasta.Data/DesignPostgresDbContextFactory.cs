using Kasta.Data.Models;
using Kasta.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kasta.Data;

public class DesignPostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresDbContext>
{
    PostgresDbContext IDesignTimeDbContextFactory<PostgresDbContext>.CreateDbContext(string[] args)
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


        var builder = new DbContextOptionsBuilder<PostgresDbContext>();
        var connectionString = KastaConfig.Instance.Database
            .GetPostgres()
            .ToConnectionString(
                includeErrorDetail: true);
        
        builder.UseApplicationServiceProvider(serviceProvider);
        builder.UseNpgsql(connectionString);

        return new PostgresDbContext(builder.Options);
    }
}