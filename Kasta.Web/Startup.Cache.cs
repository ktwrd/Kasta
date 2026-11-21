using EasyCaching.Core.Configurations;
using EasyCaching.Redis;
using EFCoreSecondLevelCacheInterceptor;
using Kasta.Shared;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using NLog;

namespace Kasta.Web;

partial class Startup
{
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
            config.DBConfig = new RedisDBOptions()
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
}