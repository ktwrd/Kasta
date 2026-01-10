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
using Vivet.AspNetCore.RequestTimeZone.Extensions;

namespace Kasta.Web;

public class Startup
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
        if (env.IsDevelopment() || _env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            // TODO migrate this to FluentScheduler, and attempt to run this every minute (and instantly).
            // if this then fails SPECIFICALLY because it can't connect to the database, then silently fail
            // otherwise, catastrophically fail.
            var context = services.GetRequiredService<ApplicationDbContext>();
            var migrations = context.Database.GetPendingMigrations().ToList();
            if (migrations.Count > 0)
            {
                var logger = LogManager.GetCurrentClassLogger();
                logger.Info("Applying the following migrations:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, migrations.Select(e => "- " + e)));
                context.Database.Migrate();
            }
        }

        // TODO configure this as a scheduled task with FluentScheduler to run instantly, and every hour
        using (var scope = app.ApplicationServices.CreateScope())
        {
            using var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CreateSession();
            try
            {
                ctx.EnsureInitialRoles();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to ensure initial roles\n{ex}");
            }
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
        services.AddScoped<GenericFileService>()
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
        services.AddDataProtection().PersistKeysToDbContext<ApplicationDbContext>();
        services.AddRequestTimeZone(opts =>
        {
            var cfg = KastaConfig.Instance;
            opts.Id = string.IsNullOrEmpty(cfg.DefaultTimezone?.Trim())
                ? "UTC"
                : cfg.DefaultTimezone;
            opts.RequestTimeZoneProviders.Add(new IPAddressRequestTimeZoneProvider());
        });
    }

    private static void ConfigureCompression(IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
                "application/json", "application/geo+json",
                "application/json", "application/pdf",
                "application/sql", "application/toml",
                "application/octet-stream", "application/xml",
                "application/wasm",
                "application/font-woff2",

                "audio/aac", "audio/flac",
                "audio/mp4", "audio/mpeg",
                "audio/ogg", "audio/opus",
                "audio/vorbis",
                "audio/midi", "audio/x-midi",

                "image/apng", "image/avif", "image/bmp",
                "image/gif", "image/heic", "image/heic-sequence",
                "image/heif", "image/heif-sequence",
                "image/jpeg", "image/jxl", "image/png",
                "image/svg+xml", "image/tiff", "image/tiff-fx",
                "image/webp", "image/vnd.microsoft.icon",

                "model/mtl", "model/mesh", "model/obj",

                "application/font-woff2",
                "font/collection", "font/otf",
                "font/sfnt", "font/ttf",
                "font/woff", "font/woff2",

                "text/javascript", "text/css",
                "text/css", "text/rtf",
                "text/plain", "text/xml",
                "text/markdown",


                "video/mp4", "video/av1",
                "video/mpeg", "video/mpeg",
                "video/ogg", "video/quicktime",
                "video/webm"
            ]);
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.SmallestSize;
        });
    }

    private static void ConfigureForwardedHeadersOptions(IServiceCollection services)
    {
        var cfg = KastaConfig.Instance;
        var parsedProxyAddresses = new List<IPAddress>();
        var ipAddressValueMapping = new List<(string, IPAddress)>()
        {
            ("any", IPAddress.Any),
            ("loopback", IPAddress.Loopback),
            ("localhost", IPAddress.Loopback),
            ("ipv6any", IPAddress.IPv6Any),
            ("ipv6loopback", IPAddress.IPv6Loopback),
        };
        foreach (var addr in cfg.Proxy?.KnownProxies.Distinct() ?? [])
        {
            var altTarget = ipAddressValueMapping
                .Where(e => e.Item1.Equals(addr, StringComparison.InvariantCultureIgnoreCase))
                .Select(e => e.Item2)
                .FirstOrDefault();
            if (altTarget != null)
            {
                parsedProxyAddresses.Add(altTarget);
            }
            else
            {
                if (!IPAddress.TryParse(addr, out var ipAddr))
                {
                    throw new InvalidOperationException($"Invalid IP Address format for Known Proxy address: \"{addr}\"");
                }
                parsedProxyAddresses.Add(ipAddr);
            }
        }
        services.Configure<ForwardedHeadersOptions>(opts =>
        {
            if (cfg.Proxy == null) return;
            foreach (var a in parsedProxyAddresses) opts.KnownProxies.Add(a);
            if (cfg.Proxy.ForwardedHeaders.HasValue)
            {
                opts.ForwardedHeaders = cfg.Proxy.ForwardedHeaders.Value;
            }
            if (cfg.Proxy.ForwardLimit.HasValue)
            {
                opts.ForwardLimit = cfg.Proxy.ForwardLimit.Value;
            }
            if (cfg.Proxy.ForwardedForHeaderName != null)
            {
                opts.ForwardedForHeaderName = cfg.Proxy.ForwardedForHeaderName;
            }
            if (cfg.Proxy.ForwardedProtoHeaderName != null)
            {
                opts.ForwardedProtoHeaderName = cfg.Proxy.ForwardedProtoHeaderName;
            }
        });
    }
    
    private void ConfigureDatabaseServices(IServiceCollection services)
    {
        // Add services to the container.
        services.AddDbContextPool<ApplicationDbContext>(
            options =>
            {
                options.ConfigureWarnings(
                    w => {
                        if (FeatureFlags.SuppressPendingModelChangesWarning) {
                            w.Ignore(RelationalEventId.PendingModelChangesWarning);
                        }
                    });
                var cfg = KastaConfig.Instance;
                var connectionString = cfg.Database.ToConnectionString();
                options.UseNpgsql(connectionString);

                if (_env.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                }
            });
        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddDefaultIdentity<UserModel>(
                options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddRoles<IdentityRole>()
                .AddUserManager<CustomUserManager<UserModel>>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
    }
    
    #region Authentication
    private static void ConfigureAuthenticationServices(IServiceCollection services)
    {
        var cfg = KastaConfig.Instance;
        if (cfg.Auth?.OAuth.Count < 1) return;
        
        var auth = services.AddAuthentication()
            .AddCookie(JwtBearerDefaults.AuthenticationScheme);
        foreach (var item in cfg.Auth!.OAuth)
        {
            auth.AddOpenIdConnect(
                item.Identifier,
                item.DisplayName,
                options =>
                {
                    ConfigureGenericOpenIdConnect(item, options);
                });
        }
    }
    private static void ConfigureGenericOpenIdConnect(
        GenericOAuthConfig config,
        OpenIdConnectOptions options)
    {
        options.RequireHttpsMetadata = false;
        options.ClientId = config.ClientId;
        options.ClientSecret = config.ClientSecret;
        options.Authority = config.Endpoint;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.ResponseMode = "query";
        options.Scope.Clear();
        foreach (var x in config.Scopes)
        {
            options.Scope.Add(x);
        }
        options.SaveTokens = true;
        // options.GetClaimsFromUserInfoEndpoint = true;
        options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
        options.TokenValidationParameters.RoleClaimType = "roles";
        if (config.UseTokenLifetime.HasValue)
        {
            options.UseTokenLifetime = config.UseTokenLifetime.Value;
        }
        foreach (var inner in config.Jwt?.Items ?? [])
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
        if (!config.ValidateIssuer)
        {
            options.TokenValidationParameters.ValidateIssuerSigningKey = false;
            options.TokenValidationParameters.SignatureValidator
                = (a, _) => new JsonWebToken(a);
        }

        // added in v0.9.2
        Task EnsureHttpsRedirect(RedirectContext ctx)
        {
            if (KastaConfig.Instance.Endpoint.StartsWith("https://") &&
                ctx.ProtocolMessage.RedirectUri.StartsWith("http://"))
            {
                ctx.ProtocolMessage.RedirectUri = "https://" + ctx.ProtocolMessage.RedirectUri[7..];
            }
            return Task.CompletedTask;
        }
        options.Events.OnRedirectToIdentityProvider += EnsureHttpsRedirect;
        options.Events.OnRedirectToIdentityProviderForSignOut += EnsureHttpsRedirect;
    }
    #endregion
    
    #region Cache
    private static void ConfigureCacheServices(IServiceCollection services)
    {
        var logger = LogManager.GetLogger(nameof(ConfigureCacheServices));
        var cfg = KastaConfig.Instance;

        if (cfg.Cache.InMemory == null && cfg.Cache.Redis == null)
        {
            cfg.Cache.InMemory ??= new();
        }
        var providerName = cfg.Cache.Redis != null ? "Redis" : "InMemory";

        services.AddEFSecondLevelCache(options =>
                    options.UseEasyCachingCoreProvider(providerName, isHybridCache: false)
                        .ConfigureLogging(true)
                        .UseCacheKeyPrefix(cfg.Cache.CachePrefix)
                        // Fallback on db if the caching provider fails (for example, if Redis is down).
                        .UseDbCallsIfCachingProviderIsDown(TimeSpan.FromMinutes(1))
            );

        services.AddEasyCaching(options =>
        {
            cfg = KastaConfig.Instance;
            var enableRedis = cfg.Cache.Redis?.Enable ?? false;
            if (enableRedis)
            {
                enableRedis = cfg.Cache.Redis!.DbConfig.Endpoints.Count >= 1;
                if (!enableRedis)
                {
                    logger.Warn("Disabling Redis Cache since no endpoints are defined.");
                }
            }
            if (enableRedis)
            {
                ConfigureRedisCache(cfg, options);
            }
            else
            {
                var memoryConfig = cfg.Cache.InMemory ?? new();
                options.UseInMemory(config =>
                {
                    config.DBConfig = new EasyCaching.InMemory.InMemoryCachingOptions()
                    {
                        ExpirationScanFrequency = memoryConfig.DbConfig.ExpirationScanFrequency,
                        SizeLimit  = memoryConfig.DbConfig.SizeLimit,
                        EnableReadDeepClone = memoryConfig.DbConfig.EnableReadDeepClone,
                        EnableWriteDeepClone = memoryConfig.DbConfig.EnableWriteDeepClone
                    };

                    config.MaxRdSecond = memoryConfig.MaxRandomSeconds;
                    config.EnableLogging = memoryConfig.EnableLogging;
                    config.LockMs = memoryConfig.LockMilliseconds;
                    config.SleepMs = memoryConfig.SleepMilliseconds;
                }, "InMemory");
            }
        });
    }
    private static void ConfigureRedisCache(KastaConfig cfg, EasyCachingOptions options)
    {
        var redisConfig = cfg.Cache.Redis!;
        options.UseRedis(config =>
        {
            config.DBConfig = new()
            {
                Database = redisConfig.DbConfig.Database,
                AsyncTimeout = redisConfig.DbConfig.AsyncTimeout,
                SyncTimeout = redisConfig.DbConfig.SyncTimeout,
                KeyPrefix = cfg.Cache.CachePrefix,

                Username = string.IsNullOrEmpty(redisConfig.DbConfig.Username) ? "" : redisConfig.DbConfig.Username,
                Password = string.IsNullOrEmpty(redisConfig.DbConfig.Password) ? "" : redisConfig.DbConfig.Password,
                IsSsl = redisConfig.DbConfig.SslEnabled,
                SslHost = redisConfig.DbConfig.SslHost,
                ConnectionTimeout = redisConfig.DbConfig.ConnectionTimeout,
                AllowAdmin = redisConfig.DbConfig.AllowAdmin,
                AbortOnConnectFail = redisConfig.DbConfig.AbortOnConnectFail,
            };
            config.DBConfig.Endpoints.Clear();
            foreach (var endpoint in redisConfig.DbConfig.Endpoints)
            {
                config.DBConfig.Endpoints.Add(new(endpoint.Host, endpoint.Port));
            }
            config.EnableLogging = redisConfig.EnableLogging;
            config.SerializerName = "Pack";

        }, "Redis")
        .WithMessagePack(so =>
        {
            so.EnableCustomResolver = true;
            var formatters = new IMessagePackFormatter[]
            {
                DBNullFormatter.Instance, // This is necessary for the null values
            };
            var formatterResolvers = new IFormatterResolver[]
            {
                NativeDateTimeResolver.Instance,
                ContractlessStandardResolver.Instance,
                StandardResolverAllowPrivate.Instance,
            };
            so.CustomResolvers = CompositeResolver.Create(formatters, formatterResolvers);
        }, "Pack");
    }
    #endregion
}