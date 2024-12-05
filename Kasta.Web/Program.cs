using System.IdentityModel.Tokens.Jwt;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Vivet.AspNetCore.RequestTimeZone.Extensions;

namespace Kasta.Web;

public static class Program
{
    public static void Main(string[] args)
    {
        IdentityModelEventSource.ShowPII = true;
        IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        var builder = WebApplication.CreateBuilder(args);

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
                options.UseNpgsql(b.ToString());
            });
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddDefaultIdentity<UserModel>(
                options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                    options.Password.RequireNonAlphanumeric = false;
                })
            .AddEntityFrameworkStores<ApplicationDbContext>();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie().SetupOpenId();

        builder.Services.AddMvc();
        builder.Services.AddScoped<S3Service>()
            .AddScoped<UploadService>()
            .AddScoped<ShortUrlService>()
            .AddScoped<FileService>()
            .AddScoped<PreviewService>()
            .AddScoped<AuditService>();
        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new BlockUserRegisterAttribute());
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDataProtection().PersistKeysToDbContext<ApplicationDbContext>();
        if (string.IsNullOrEmpty(FeatureFlags.DefaultRequestTimezone))
        {
            builder.Services.AddRequestTimeZone("UTC");
        }
        else
        {
            builder.Services.AddRequestTimeZone(FeatureFlags.DefaultRequestTimezone);
        }

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

    private static AuthenticationBuilder SetupOpenId(this AuthenticationBuilder builder)
    {
        if (!FeatureFlags.OpenIdEnable) return builder;
        var b = builder;
        if (FeatureFlags.JwtEnable)
        {
            b = b.AddJwtBearer(opt =>
            {
                SymmetricSecurityKey? signingKey = null;
                if (!string.IsNullOrEmpty(FeatureFlags.JwtIssuerSigningKeyLocation))
                {
                    if (File.Exists(FeatureFlags.JwtIssuerSigningKeyLocation))
                    {
                        signingKey = new(File.ReadAllBytes(FeatureFlags.JwtIssuerSigningKeyLocation));
                    }
                }
                if (!string.IsNullOrEmpty(FeatureFlags.JwtIssuerSigningKeyBase64) && signingKey == null)
                {
                    signingKey = new(Convert.FromBase64String(FeatureFlags.JwtIssuerSigningKeyBase64));
                }
                opt.TokenValidationParameters = new()
                {
                    ValidateIssuer = FeatureFlags.JwtValidateIssuer,
                    ValidateAudience = FeatureFlags.JwtValidateAudience,
                    ValidateLifetime = FeatureFlags.JwtValidateLifetime,
                    ValidateIssuerSigningKey = FeatureFlags.JwtValidateIssuerSigningKey,
                    ValidIssuer = FeatureFlags.JwtValidIssuer,
                    ValidAudience = FeatureFlags.JwtValidAudience,
                    IssuerSigningKey = signingKey
                };
            });
        }
        return b
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.UseSecurityTokenValidator = false;
            options.Scope.Clear();
            foreach (var s in FeatureFlags.OpenIdScopes.Split(' '))
            {
                options.Scope.Add(s);
            }
            options.Authority = FeatureFlags.OpenIdConfigurationUrl;
            options.ClientId = FeatureFlags.OpenIdClientId;
            options.ClientSecret = FeatureFlags.OpenIdClientSecret;

            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.ResponseType = FeatureFlags.OpenIdResponseType;

            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;

            options.MapInboundClaims = false;
            options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
            options.TokenValidationParameters.RoleClaimType = "roles";
        });
    }
}