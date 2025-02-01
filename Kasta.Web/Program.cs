using Kasta.Shared;
using Microsoft.IdentityModel.Logging;
using NLog.Web;

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
}