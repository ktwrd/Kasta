using Kasta.Shared;
using Microsoft.IdentityModel.Logging;
using NLog;
using NLog.Web;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Trace = System.Diagnostics.Trace;

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