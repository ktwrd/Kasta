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
            .AddScoped<FileWebService>();
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
}