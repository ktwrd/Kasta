using System.IdentityModel.Tokens.Jwt;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Npgsql;
using Vivet.AspNetCore.RequestTimeZone.Extensions;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Kasta.Web.Helpers;
using System.Diagnostics;

namespace Kasta.Web;

public static class Program
{
    public static bool IsDevelopment
    {
        get
        {
            return string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "development", StringComparison.InvariantCultureIgnoreCase);
        }
    }
    public static bool IsDocker { get; private set; }
    public static void Main(string[] args)
    {
        IsDocker = args.FirstOrDefault() == "docker";
        if (IsDocker)
        {
            Environment.SetEnvironmentVariable("_KASTA_RUNNING_IN_DOCKER", "true");
        }
        if (IsDevelopment)
        {
            IdentityModelEventSource.ShowPII = true;
            IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        }
        if (!string.IsNullOrEmpty(FeatureFlags.XmlConfigLocation))
        {
            if (!File.Exists(FeatureFlags.XmlConfigLocation))
            {
                new KastaConfig().WriteToFile(FeatureFlags.XmlConfigLocation);
                Console.WriteLine($"Wrote blank config file to {FeatureFlags.XmlConfigLocation}");
            }
        }
        var builder = WebApplication.CreateBuilder(args);
        if (!string.IsNullOrEmpty(FeatureFlags.SentryDsn))
        {
            builder.WebHost.UseSentry(opts =>
            {
                opts.Dsn = FeatureFlags.SentryDsn;
                opts.SendDefaultPii = true;
                opts.MinimumBreadcrumbLevel = LogLevel.Trace;
                opts.MinimumEventLevel = LogLevel.Warning;
                opts.AttachStacktrace = true;
                opts.DiagnosticLevel = SentryLevel.Debug;
                opts.TracesSampleRate = 1.0;
                opts.MaxRequestBodySize = Sentry.Extensibility.RequestSize.Always;
                #if DEBUG
                opts.Debug = true;
                #else
                opts.Debug = false;
                #endif
            });
        }

        // Add services to the container.
        builder.Services.AddDbContextPool<ApplicationDbContext>(
            options =>
            {
                var cfg = KastaConfig.Get();
                var connectionString = cfg.Database.ToConnectionString();
                options.UseNpgsql(connectionString);

                if (IsDevelopment)
                {
                    options.EnableSensitiveDataLogging();
                }
            });
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddDefaultIdentity<UserModel>(
                options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        
        InitializeOAuth(builder);
        builder.Services.AddMvc();
        builder.Services.AddScoped<S3Service>()
            .AddScoped<UploadService>()
            .AddScoped<ShortUrlService>()
            .AddScoped<FileService>()
            .AddScoped<PreviewService>()
            .AddScoped<AuditService>()
            .AddScoped<FileWebService>()
            .AddScoped<LinkShortenerWebService>()
            .AddScoped<TimeZoneService>()
            .AddScoped<MailboxService>();
        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new BlockUserRegisterAttribute());
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDataProtection().PersistKeysToDbContext<ApplicationDbContext>();
        builder.Services.AddRequestTimeZone(opts =>
        {
            var cfg = KastaConfig.Get();
            if (string.IsNullOrEmpty(cfg.DefaultTimezone))
            {
                opts.Id = "UTC";
            }
            else
            {
                opts.Id = cfg.DefaultTimezone;   
            }
            opts.RequestTimeZoneProviders.Add(new IPAddressRequestTimeZoneProvider());
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var context = services.GetRequiredService<ApplicationDbContext>();
                if (context.Database.GetPendingMigrations().Any())
                {
                    context.Database.Migrate();
                }
            }
        }

        using (var scope = app.Services.CreateScope())
        {
            using (var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CreateSession())
            {
                var trans = ctx.Database.BeginTransaction();
                try
                {
                    var s = ctx.GetSystemSettings();
                    ctx.SaveChanges();
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to insert global preferences.\n{ex}");
                    trans.Rollback();
                }
            }
        }

        app.UseRequestTimeZone();

        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
            
        app.MapRazorPages();

        app.Run();
    }
    private static void InitializeOAuth(WebApplicationBuilder builder)
    {
        var cfg = KastaConfig.Get();
        if (cfg.Auth?.OAuth.Count < 1) return;
        
        var auth = builder.Services.AddAuthentication()
            .AddCookie(JwtBearerDefaults.AuthenticationScheme);
        foreach (var item in cfg.Auth!.OAuth)
        {
            auth.AddOpenIdConnect(
                item.Identifier,
                item.DisplayName,
                options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.ClientId = item.ClientId;
                    options.ClientSecret = item.ClientSecret;
                    options.Authority = item.Endpoint;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.ResponseMode = "query";
                    options.Scope.Clear();
                    foreach (var x in item.Scopes)
                    {
                        options.Scope.Add(x);
                    }
                    options.SaveTokens = true;
                    // options.GetClaimsFromUserInfoEndpoint = true;
                    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    options.TokenValidationParameters.RoleClaimType = "roles";
                    foreach (var inner in item.Jwt?.Items ?? [])
                    {
                        switch (inner.InternalName)
                        {
                            case "name":
                                options.TokenValidationParameters.NameClaimType = inner.JwtValue;
                                break;
                            case "role":
                                options.TokenValidationParameters.RoleClaimType = inner.JwtValue;
                                break;
                        }
                    }
                    if (item.ValidateIssuer == false)
                    {
                        options.TokenValidationParameters.ValidateIssuerSigningKey = false;
                        options.TokenValidationParameters.SignatureValidator = (a, b) =>
                        {
                            return new JsonWebToken(a);
                        };
                    }
                });
        }
    }
}