using Microsoft.EntityFrameworkCore;
using kate.FileShare.Data;
using kate.FileShare.Data.Models;
using kate.FileShare.Services;

namespace kate.FileShare;

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
                })
            .AddEntityFrameworkStores<ApplicationDbContext>();
        builder.Services.AddMvc();
        builder.Services.AddScoped<S3Service>()
            .AddScoped<UploadService>()
            .AddScoped<ShortUrlService>()
            .AddScoped<FileService>();
        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
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