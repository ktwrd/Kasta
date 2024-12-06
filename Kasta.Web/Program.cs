using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Npgsql;
using Vivet.AspNetCore.RequestTimeZone.Extensions;

namespace Kasta.Web;

public class Program
{
    public static void Main(string[] args)
    {
        IdentityModelEventSource.ShowPII = true;
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
            .AddEntityFrameworkStores<ApplicationDbContext>();
        if (FeatureFlags.OpenIdEnable)
        {
            builder.Services.AddAuthentication().AddOpenIdConnect(
                options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.ClientId = FeatureFlags.OpenIdClientId;
                    options.ClientSecret = FeatureFlags.OpenIdClientSecret;
                    options.Authority = FeatureFlags.OpenIdEndpoint;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    options.TokenValidationParameters.RoleClaimType = "roles";
                });

        }
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

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
            
        app.MapRazorPages();

        app.Run();
    }
}