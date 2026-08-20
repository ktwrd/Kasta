using Kasta.Data;
using Kasta.Shared;
using Kasta.Web.Services;
using Microsoft.AspNetCore.DataProtection;
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

        app.UseRequestTimeZone();

        app.UseStaticFiles();
        if (KastaConfig.Instance.Proxy?.PathBase?.StartsWith('/') == true)
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
                    if (context.Request.Path.StartsWithSegments(
                            KastaConfig.Instance.Proxy.PathBase,
                            out var remainder))
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
        services.AddHostedService<AppStartupService>();
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
        services.AddDataProtection()
            .PersistKeysToDbContext<KastaDbContext>();
    }
}