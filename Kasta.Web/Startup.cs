using EFCoreSecondLevelCacheInterceptor;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Helpers;
using Kasta.Web.Services;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NLog;
using System.IO.Compression;
using System.Net;
using EasyCaching.Core.Configurations;
using Kasta.Shared.ConfigEditions;
using Microsoft.EntityFrameworkCore.Internal;
using Vivet.AspNetCore.RequestTimeZone.Extensions;

namespace Kasta.Web;

public partial class Startup
{
    private readonly string _contentRootPath;
    private readonly IWebHostEnvironment _env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        _env = env;
        _contentRootPath = env.ContentRootPath;
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Configure the HTTP request pipeline.
        var isDev = env.IsDevelopment() || _env.IsDevelopment();
        if (isDev)
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }
        
        if (isDev || KastaConfig.Instance.Database.GetProvider() == DatabaseConfigElement.DatabaseProviderKind.Sqlite)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;
            // TODO migrate this to FluentScheduler, and attempt to run this every minute (and instantly).
            // if this then fails SPECIFICALLY because it can't connect to the database, then silently fail
            // otherwise, catastrophically fail.
            var context = services.GetRequiredService<KastaDbContext>();
            var migrations = context.Database.GetPendingMigrations().ToList();
            var logger = LogManager.GetCurrentClassLogger();
            if (migrations.Count > 0)
            {
                logger.Info("Applying the following migrations:"
                            + Environment.NewLine
                            + string.Join(Environment.NewLine, migrations.Select(e => "- " + e)));
            }
            context.Database.Migrate();
            context.SaveChanges();
            logger.Info("Finished applying migrations");
        }

        // TODO configure this as a scheduled task with FluentScheduler to run instantly, and every hour
        var dbContextFactory = app.ApplicationServices.GetRequiredService<IDbContextFactory<KastaDbContext>>();
        using var db = dbContextFactory.CreateDbContext();
        try
        {
            db.EnsureInitialRoles();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to ensure initial roles\n{ex}");
        }
        using (var scope = app.ApplicationServices.CreateScope())
        {
            try
            {
                scope.ServiceProvider.GetRequiredService<SystemSettingsProxy>()
                    .EnsureInitialized();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to insert global preferences.\n{ex}");
            }
        }

        app.UseRequestTimeZone();

        app.UseStaticFiles();
        if (KastaConfig.Instance.Proxy != null
            && (KastaConfig.Instance.Proxy.PathBase?.StartsWith('/') ?? false))
        {
            if (KastaConfig.Instance.Proxy.IsProxyTrimmingPathBase)
            {
                app.Use((context, next) =>
                {
                    context.Request.PathBase = new PathString(KastaConfig.Instance.Proxy.PathBase);
                    return next(context);
                });
            }
            else if (KastaConfig.Instance.Proxy.IsProxyPrependingPathBase)
            {
                app.Use((context, next) =>
                {
                    if (context.Request.Path.StartsWithSegments(KastaConfig.Instance.Proxy.PathBase, out var remainder))
                    {
                        context.Request.Path = remainder;
                    }

                    return next(context);
                });
            }
            else
            {
                app.UsePathBase(KastaConfig.Instance.Proxy.PathBase);
            }
        }
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpointBuilder =>
        {
            endpointBuilder.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            endpointBuilder.MapRazorPages();
        });
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(KastaConfig.Instance);
        ConfigureForwardedHeadersOptions(services);
        ConfigureDatabaseServices(services);
        ConfigureAuthenticationServices(services);
        ConfigureCacheServices(services);
        ConfigureCompression(services);
        services.AddMvc();
        services.AddScoped<UserService>()
                .AddScoped<GenericFileService>()
                .AddScoped<SystemSettingsProxy>()
                .AddScoped<S3Service>()
                .AddScoped<UploadService>()
                .AddScoped<ShortUrlService>()
                .AddScoped<FileService>()
                .AddScoped<PreviewService>()
                .AddScoped<AuditService>()
                .AddScoped<FileWebService>()
                .AddScoped<LinkShortenerWebService>()
                .AddScoped<TimeZoneService>()
                .AddScoped<MailboxService>();
        services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new BlockUserRegisterAttribute());
        });
        services.AddHttpContextAccessor();
        services.AddRequestTimeZone(ConfigureTimeZone);
    }
}