using Kasta.Shared;
using Microsoft.IdentityModel.Logging;
using NLog;
using NLog.Web;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Trace = System.Diagnostics.Trace;
using MinDataRate = Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate;

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
        if (IsDevelopment || FeatureFlags.ShowPrivateInformationWithAspNet)
        {
            IdentityModelEventSource.ShowPII = true;
            IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        }

        InitializeNLog();
        CheckConfiguration();
        RunServer(ref args);
    }

    private static void RunServer(ref string[] args)
    {
        var h = Host.CreateDefaultBuilder(args)
            .UseNLog().ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseStartup<Startup>();
                
                var cfg = KastaConfig.Get();
                webBuilder.UseKestrel(opts =>
                {
                    if (cfg?.Kestrel?.Limits != null)
                    {
                        var limits = cfg.Kestrel.Limits;
                        if (limits.MaxResponseBufferSize != null)
                        {
                            if (limits.MaxResponseBufferSize == -1)
                            {
                                opts.Limits.MaxResponseBufferSize = -1;
                            }
                            else
                            {
                                opts.Limits.MaxResponseBufferSize = limits.MaxResponseBufferSize.Value;
                            }
                        }
                        if (limits.MaxRequestBufferSize != null)
                        {
                            if (limits.MaxRequestBufferSize == -1)
                            {
                                opts.Limits.MaxRequestBufferSize = null;
                            }
                            else
                            {
                                opts.Limits.MaxRequestBufferSize = limits.MaxRequestBufferSize.Value;
                            }
                        }
                        if (limits.MaxRequestLineSize != null && limits.MaxRequestLineSize.HasValue)
                        {
                            opts.Limits.MaxRequestLineSize = limits.MaxRequestLineSize.Value;
                        }
                        if (limits.MaxRequestHeadersTotalSize != null && limits.MaxRequestHeadersTotalSize.HasValue)
                        {
                            opts.Limits.MaxRequestHeadersTotalSize = limits.MaxRequestHeadersTotalSize.Value;
                        }
                        if (limits.MaxRequestHeaderCount != null && limits.MaxRequestHeaderCount.HasValue)
                        {
                            opts.Limits.MaxRequestHeaderCount = limits.MaxRequestHeaderCount.Value;
                        }
                        if (limits.MaxRequestBodySize != null)
                        {
                            if (limits.MaxRequestBodySize == -1)
                            {
                                opts.Limits.MaxRequestBodySize = null;
                            }
                            else
                            {
                                opts.Limits.MaxRequestBodySize = limits.MaxRequestBodySize.Value;
                            }
                        }
                        if (limits.KeepAliveTimeout != null)
                        {
                            opts.Limits.KeepAliveTimeout = limits.KeepAliveTimeout.ToTimeSpan();
                        }
                        if (limits.RequestHeadersTimeout != null)
                        {
                            opts.Limits.RequestHeadersTimeout = limits.RequestHeadersTimeout.ToTimeSpan();
                        }
                        if (limits.MaxConcurrentConnections != null)
                        {
                            if (limits.MaxConcurrentConnections == -1)
                            {
                                opts.Limits.MaxConcurrentConnections = null;
                            }
                            else
                            {
                                opts.Limits.MaxConcurrentConnections = limits.MaxConcurrentConnections.Value;
                            }
                        }
                        if (limits.MaxConcurrentUpgradedConnections != null)
                        {
                            if (limits.MaxConcurrentUpgradedConnections == -1)
                            {
                                opts.Limits.MaxConcurrentUpgradedConnections = null;
                            }
                            else
                            {
                                opts.Limits.MaxConcurrentUpgradedConnections = limits.MaxConcurrentUpgradedConnections.Value;
                            }
                        }

                        if (limits.EnforceMinRequestBodyDataRate == false)
                        {
                            opts.Limits.MinRequestBodyDataRate = null;
                        }
                        else if (limits.MinRequestBodyDataRate != null)
                        {
                            var dataRate = limits.MinRequestBodyDataRate;
                            opts.Limits.MinRequestBodyDataRate = new MinDataRate(dataRate.BytesPerSecond, dataRate.GracePeriod.ToTimeSpan());
                        }

                        if (limits.EnforceMinResponseDataRate == false)
                        {
                            opts.Limits.MinResponseDataRate = null;
                        }
                        else if (limits.MinResponseDataRate != null)
                        {
                            var dataRate = limits.MinResponseDataRate;
                            opts.Limits.MinResponseDataRate = new MinDataRate(dataRate.BytesPerSecond, dataRate.GracePeriod.ToTimeSpan());
                        }
                    }
                });
                if (!string.IsNullOrEmpty(FeatureFlags.SentryDsn))
                {
                    webBuilder.UseSentry(opts =>
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
            });
        h.RunConsoleAsync().Wait();
    }

    private static void InitializeNLog()
    {
        Trace.WriteLine($"Initialize NLog");
        var relativeConfigurationLocation = Path.Combine(Environment.CurrentDirectory, "nlog.config");
        if (File.Exists(relativeConfigurationLocation))
        {
            Trace.WriteLine("Loading configuration from " + relativeConfigurationLocation);
            LogManager.Setup().LoadConfigurationFromFile(relativeConfigurationLocation);
        }
        else
        {
            LogManager.Setup().LoadConfigurationFromAssemblyResource(typeof(Program).Assembly, "nlog.config");
        }

        if (!string.IsNullOrEmpty(FeatureFlags.SentryDsn))
        {
            LogManager.Configuration.AddSentry(
                opts =>
                {
                    opts.Dsn = FeatureFlags.SentryDsn;
                    opts.SendDefaultPii = true;
                    opts.MinimumBreadcrumbLevel = NLog.LogLevel.Trace;
                    opts.MinimumEventLevel = NLog.LogLevel.Warn;
                    opts.AttachStacktrace = true;
                    opts.DiagnosticLevel = SentryLevel.Debug;
                    opts.TracesSampleRate = 1.0;
#if DEBUG
                    opts.Debug = true;
#else
                        opts.Debug = false;
#endif
                });
        }
    }

    private static void CheckConfiguration()
    {
        var logger = LogManager.GetLogger(nameof(CheckConfiguration));
        try
        {
            // We know for sure that the default configuration location used
            // by Kasta is "/config", and that this will only happen on Docker.
            //
            // This is here just to be sure that the creation of the config
            // directory will not fail.
            if (IsDocker)
            {
                if (!Directory.Exists("/config"))
                {
                    Directory.CreateDirectory("/config");
                }
            }
            
            logger.Info($"Using File: {FeatureFlags.XmlConfigLocation}");
            if (string.IsNullOrEmpty(FeatureFlags.XmlConfigLocation))
            {
                logger.Error($"Environment Variable CONFIG_LOCATION has not been set!!!");
                Environment.Exit(1);
                return;
            }
            
            // Create parent directory if it doesn't exist.
            var parentDirectory = Path.GetDirectoryName(FeatureFlags.XmlConfigLocation);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                if (!Directory.Exists(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }
            }

            if (!File.Exists(FeatureFlags.XmlConfigLocation))
            {
                var selfType = typeof(Program);
                logger.Warn("Configuration file does not exist!! Creating a blank one, PLEASE POPULATE IT !!!");
                File.WriteAllText(FeatureFlags.XmlConfigLocation, string.Empty);
                if (TryGetExampleConfigurationStream(out var exampleConfigStream))
                {
                    using var file = new FileStream(
                        FeatureFlags.XmlConfigLocation,
                        FileMode.OpenOrCreate,
                        FileAccess.Write,
                        FileShare.Read);

                    file.SetLength(0);
                    file.Seek(0, SeekOrigin.Begin);
                    exampleConfigStream!.CopyTo(file);
                }
                else
                {
                    logger.Fatal($"Couldn't find embedded resource {selfType.Namespace}.config.example.xml");
                }
                Environment.Exit(1);
                return;
            }
            else
            {
                logger.Info($"Configuration file found! ({FeatureFlags.XmlConfigLocation})");
            }
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Failed to check configuration!");
        }
    }

    private static bool TryGetExampleConfigurationStream(out Stream? stream)
    {
        var current = typeof(Program);
        foreach (var resource in current.Assembly.GetManifestResourceNames())
        {
            if (resource.ToLower().Trim().EndsWith(".config.example.xml"))
            {
                stream = current.Assembly.GetManifestResourceStream(resource);
                if (stream != null)
                    return true;
            }
        }
        stream = null;
        return false;
    }
}