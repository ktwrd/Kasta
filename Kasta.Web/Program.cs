using Microsoft.EntityFrameworkCore;
using Kasta.Web.Data;
using Kasta.Web.Data.Models;
using Kasta.Web.Services;

namespace Kasta.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddDbContextPool<ApplicationDbContext>(
            options =>
            {
                options.UseNpgsql(FeatureFlags.ConnectionString);
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
        builder.Services.AddMvc();
        builder.Services.AddScoped<S3Service>()
            .AddScoped<UploadService>()
            .AddScoped<ShortUrlService>()
            .AddScoped<FileService>()
            .AddScoped<PreviewService>();
        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new BlockUserRegisterAttribute());
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