using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kasta.Web;

partial class Startup
{
    private void ConfigureDatabaseServices(IServiceCollection services)
    {
        // Add services to the container.
        if (KastaConfig.Instance.Database.GetProvider() == DatabaseConfigElement.DatabaseProviderKind.Postgres ||
            KastaConfig.Instance.Database.UseLegacyPostgresSettings())
        {
            ConfigureDatabase<PostgresDbContext>(services);
        }
        else
        {
            ConfigureDatabase<SqliteDbContext>(services);
        }
        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddDefaultIdentity<UserModel>(
                options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddRoles<IdentityRole>()
                .AddUserManager<CustomUserManager<UserModel>>()
                .AddEntityFrameworkStores<KastaDbContext>();
    }

    private void ConfigureDatabase<TContextService>(IServiceCollection services)
        where TContextService : KastaDbContext
    {
        services.AddDbContextPool<KastaDbContext, TContextService>(ConfigureApplicationDbContext)
                .AddPooledDbContextFactory<KastaDbContext>(ConfigureApplicationDbContext);
    }

    private void ConfigureApplicationDbContext(DbContextOptionsBuilder options)
    {
        options.ConfigureWarnings(w => {
            if (FeatureFlags.SuppressPendingModelChangesWarning) {
                w.Ignore(RelationalEventId.PendingModelChangesWarning);
            }
        });
        var cfg = KastaConfig.Instance;
        
        var connectionString = cfg.Database.GetProvider() == DatabaseConfigElement.DatabaseProviderKind.Postgres
            ? cfg.Database.GetPostgres().ToConnectionString()
            : cfg.Database.GetSqlite().ToConnectionString();
        if (cfg.Database.GetProvider() == DatabaseConfigElement.DatabaseProviderKind.Postgres)
        {
            options.UseNpgsql(connectionString);
        }
        else
        {
            options.AddInterceptors(new SqliteWalInterceptor());
            options.UseSqlite(connectionString);
        }

        if (_env.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
        }
    }
}