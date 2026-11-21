using Humanizer;
using Kasta.Data;
using Kasta.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DatabaseProviderKind = Kasta.Shared.DatabaseConfigElement.DatabaseProviderKind;
// ReSharper disable ConvertToPrimaryConstructor
#pragma warning disable CA2254
#pragma warning disable CA1873

namespace Kasta.Web.Services;

public class AppStartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _webHostEnv;
    private readonly KastaConfig _config;
    private readonly ILogger<AppStartupService> _logger;

    public AppStartupService(IServiceProvider serviceProvider, ILogger<AppStartupService> logger)
    {
        _serviceProvider = serviceProvider;
        _webHostEnv = serviceProvider.GetRequiredService<IWebHostEnvironment>();
        _config = serviceProvider.GetRequiredService<KastaConfig>();
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        try
        {
            await ApplyDatabaseMigrations(scope.ServiceProvider, cancellationToken);
        }
        catch (Exception e)
        {
            throw new KastaInitialiseException("Failed to apply one or more database migrations", e);
        }

        try
        {
            await CreateIdentityRoles(scope.ServiceProvider, cancellationToken);
        }
        catch (Exception e)
        {
            throw new KastaInitialiseException("Failed to initialise required roles", e);
        }
    }

    private async Task ApplyDatabaseMigrations(IServiceProvider services, CancellationToken ct)
    {
        if (!_webHostEnv.IsDevelopment() && _config.Database.GetProvider() != DatabaseProviderKind.Postgres)
            return;
        var db = services.GetRequiredService<KastaDbContext>();
        var migrations = await db.Database.GetPendingMigrationsAsync(ct);
        var migrationsArr = migrations.ToArray();
        if (migrationsArr.Length < 1)
        {
            _logger.LogInformation("No pending migrations");
            return;
        }
        _logger.LogInformation("Applying " + ("migration".ToQuantity(migrationsArr.Length)) + "\n"
            + string.Join("\n", migrationsArr));
        await db.Database.MigrateAsync(ct);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Success!");
    }

    private async Task CreateIdentityRoles(IServiceProvider services, CancellationToken ct)
    {
        _logger.LogInformation("Creating required roles in db (if required)");
        var db = services.GetRequiredService<KastaDbContext>();
        var roleNames = db.Roles.Select(e => e.Name).Distinct().ToArray()
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Cast<string>()
            .Select(e => e.ToUpper())
            .ToArray();
        var items = RoleKind.ToList()
            .Where(e => !roleNames.Contains(e.Name, StringComparer.OrdinalIgnoreCase))
            .Select(item => new IdentityRole()
            {
                Id = Guid.NewGuid().ToString(),
                Name = item.Name,
                NormalizedName = item.Name.ToUpper(),
                ConcurrencyStamp = null
            })
            .ToArray();
        
        if (items.Length < 1)
        {
            return;
        }
        
        _logger.LogInformation("Adding " + "role".ToQuantity(items.Length));
        await using var trans = await db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var item in items)
            {
                try
                {
                    await db.AddAsync(item, ct);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role \"{item.Name}\"\nId={item.Id}\nNormalizedName={item.NormalizedName}",
                        e);
                }
            }
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to save changes", e);
            }

            try
            {
                await trans.CommitAsync(ct);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to commit changes", e);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialise database roles!");
            await trans.RollbackAsync(ct);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
