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
    public static void Main(string[] args)
    {
        if (IsDevelopment)
        {
            IdentityModelEventSource.ShowPII = true;
            IdentityModelEventSource.LogCompleteSecurityArtifact = true;
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
                var b = new NpgsqlConnectionStringBuilder();
                b.Host = FeatureFlags.DatabaseHost;
                b.Port = FeatureFlags.DatabasePort;
                b.Username = FeatureFlags.DatabaseUser;
                b.Password = FeatureFlags.DatabasePassword;
                b.Database = FeatureFlags.DatabaseName;
                b.IncludeErrorDetail = true;
                options.UseNpgsql(b.ToString());

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
        if (FeatureFlags.OpenIdEnable)
        {
            builder.Services.AddAuthentication()
            .AddCookie(JwtBearerDefaults.AuthenticationScheme).AddOpenIdConnect(
                string.IsNullOrEmpty(FeatureFlags.OpenIdIdentifier) ? OpenIdConnectDefaults.AuthenticationScheme : FeatureFlags.OpenIdIdentifier,
                string.IsNullOrEmpty(FeatureFlags.OpenIdDisplayName) ? null : FeatureFlags.OpenIdDisplayName,
                options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.ClientId = FeatureFlags.OpenIdClientId;
                    options.ClientSecret = FeatureFlags.OpenIdClientSecret;
                    options.Authority = FeatureFlags.OpenIdEndpoint;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.ResponseMode = "query";
                    options.Scope.Clear();
                    foreach (var x in FeatureFlags.OpenIdScopes.Split(' '))
                    {
                        options.Scope.Add(x);
                    }
                    options.SaveTokens = true;
                    // options.GetClaimsFromUserInfoEndpoint = true;
                    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    options.TokenValidationParameters.RoleClaimType = FeatureFlags.JwtRoleClaimType;
                    if (FeatureFlags.OpenIdValidateIssuer == false)
                    {
                        options.TokenValidationParameters.ValidateIssuerSigningKey = false;
                        options.TokenValidationParameters.SignatureValidator = (a, b) =>
                        {
                            return new JsonWebToken(a);
                        };
                    }
                });
        }
        builder.Services.AddMvc();
        builder.Services.AddScoped<S3Service>()
            .AddScoped<UploadService>()
            .AddScoped<ShortUrlService>()
            .AddScoped<FileService>()
            .AddScoped<PreviewService>()
            .AddScoped<AuditService>()
            .AddScoped<FileWebService>()
            .AddScoped<LinkShortenerWebService>()
            .AddScoped<TimeZoneService>();
        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new BlockUserRegisterAttribute());
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDataProtection().PersistKeysToDbContext<ApplicationDbContext>();
        builder.Services.AddRequestTimeZone(opts =>
        {
            if (string.IsNullOrEmpty(FeatureFlags.DefaultRequestTimezone))
            {
                opts.Id = "UTC";
            }
            else
            {
                opts.Id = FeatureFlags.DefaultRequestTimezone;   
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
}