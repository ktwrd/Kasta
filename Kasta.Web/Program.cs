using Kasta.Shared;
using Microsoft.IdentityModel.Logging;
using NLog;
using NLog.Web;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Trace = System.Diagnostics.Trace;
using MinDataRate = Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate;
using Kasta.Web.Helpers;
using Sentry.NLog;

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
                
                var cfg = KastaConfig.Instance;
                webBuilder.UseKestrel(opts => opts.FromConfiguration(cfg));
                if (!string.IsNullOrEmpty(FeatureFlags.SentryDsn))
                {
                    webBuilder.UseSentry(opts =>
                    {
                        SetSentryOptions(opts);
                        opts.MinimumBreadcrumbLevel = LogLevel.Trace;
                        opts.MinimumEventLevel = LogLevel.Warning;
                        opts.MaxRequestBodySize = Sentry.Extensibility.RequestSize.Always;
                    });
                }
            });
        h.RunConsoleAsync().Wait();
    }

    private static void InitializeNLog()
    {
        const string prefix = "[InitializeNLog]";
        var relativeConfigurationLocation = Path.Combine(Environment.CurrentDirectory, "nlog.config");
        if (File.Exists(relativeConfigurationLocation))
        {
            Console.WriteLine(prefix + " Loading configuration from " + relativeConfigurationLocation);
            LogManager.Setup().LoadConfigurationFromFile(relativeConfigurationLocation);
        }
        else
        {
            Console.WriteLine(prefix + " Loading configuration from embedded resource");
            LogManager.Setup().LoadConfigurationFromAssemblyResource(typeof(Program).Assembly, "nlog.config");
        }

        if (!string.IsNullOrEmpty(FeatureFlags.SentryDsn))
        {
            Console.WriteLine(prefix + " Enabling Sentry Integration: " + FeatureFlags.SentryDsn);
            LogManager.Configuration?.AddSentry(SetSentryOptions);
        }
    }
    private static void SetSentryOptions(SentryOptions opts)
    {
        opts.Dsn = FeatureFlags.SentryDsn;
        opts.Release = typeof(Program).Assembly.GetName().Version?.ToString();
        opts.SendDefaultPii = true;
        opts.AttachStacktrace = true;
        opts.DiagnosticLevel = SentryLevel.Debug;
        opts.TracesSampleRate = 1.0;
#if DEBUG
        opts.Debug = true;
#else
        opts.Debug = false;
#endif
        if (File.Exists(FeatureFlags.XmlConfigLocation))
        {
            opts.FromConfiguration();
        }
        if (opts is SentryNLogOptions nlogOptions)
        {
            if (File.Exists(FeatureFlags.XmlConfigLocation))
            {
                nlogOptions.FromConfiguration();
            }
            else
            {
                nlogOptions.MinimumBreadcrumbLevel = NLog.LogLevel.Trace;
                nlogOptions.MinimumEventLevel = NLog.LogLevel.Warn;
            }
        }
    }
    private static void CheckConfiguration()
    {
        var logger = LogManager.GetLogger(nameof(CheckConfiguration));
#if !DEBUG
        try
        {
#endif
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
                logger.Error($"Environment Variable \"{FeatureFlags.Keys.XmlConfigLocation}\" has not been set!!!");
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
#if !DEBUG
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Failed to check configuration!");
            Environment.Exit(1);
        }
#endif
    }

    private static bool TryGetExampleConfigurationStream(out Stream? stream)
    {
        stream = null;

        var current = typeof(Program);
        var resourceName = current.Assembly.GetManifestResourceNames().FirstOrDefault(e => e.Trim().EndsWith(".config.example.xml", StringComparison.OrdinalIgnoreCase));
        if (resourceName == null) return false;

        stream = current.Assembly.GetManifestResourceStream(resourceName);
        return stream != null;
    }
}