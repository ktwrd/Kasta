using Kasta.Shared;
using Microsoft.AspNetCore.Server.Kestrel.Core;

using XmlSentryLevel = Kasta.Shared.SentryConfig.XmlSentryLevel;
using XmlNLogLogLevel = Kasta.Shared.SentryNLogConfigElement.XmlLogLevel;
using Sentry.NLog;

namespace Kasta.Web.Helpers;

public static class ConfigExtensions
{
    public static void FromConfiguration(this KestrelServerOptions opts, KastaConfig config)
    {
        if (config?.Kestrel?.Limits == null) return;

        var limits = config.Kestrel.Limits;
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

    public static void FromConfiguration(this SentryOptions opts) => opts.FromConfiguration(KastaConfig.Instance);
    public static void FromConfiguration(this SentryOptions opts, KastaConfig cfg)
    {
        if (cfg.Sentry == null)
        {
#if DEBUG
            opts.TracesSampleRate = 1.0;
#endif
            return;
        }

        if (cfg.Sentry.SampleRate != null && cfg.Sentry.SampleRate.HasValue)
        {
            if (cfg.Sentry.SampleRate < 0.0f)
            {
                opts.SampleRate = null;
            }
            else if (cfg.Sentry.SampleRate <= 1.0f)
            {
                opts.SampleRate = cfg.Sentry.SampleRate.Value;
            }
            else if (cfg.Sentry.SampleRate > 1.0f)
            {
                opts.SampleRate = cfg.Sentry.SampleRate.Value % 1.0f;
            }
        }

        if (cfg.Sentry.ProfilesSampleRate != null && cfg.Sentry.ProfilesSampleRate.HasValue)
        {
            if (cfg.Sentry.ProfilesSampleRate < 0.0f)
            {
                opts.ProfilesSampleRate = null;
            }
            else if (cfg.Sentry.ProfilesSampleRate <= 1.0f)
            {
                opts.ProfilesSampleRate = cfg.Sentry.ProfilesSampleRate.Value;
            }
            else if (cfg.Sentry.ProfilesSampleRate > 1.0f)
            {
                opts.ProfilesSampleRate = cfg.Sentry.ProfilesSampleRate.Value % 1.0f;
            }
        }

        if (cfg.Sentry.TracesSampleRate != null && cfg.Sentry.TracesSampleRate.HasValue)
        {
            if (cfg.Sentry.TracesSampleRate < 0.0f)
            {
                opts.TracesSampleRate = null;
            }
            else if (cfg.Sentry.TracesSampleRate <= 1.0f)
            {
                opts.TracesSampleRate = cfg.Sentry.TracesSampleRate.Value;
            }
            else if (cfg.Sentry.TracesSampleRate > 1.0f)
            {
                opts.TracesSampleRate = cfg.Sentry.TracesSampleRate.Value % 1.0f;
            }
        }
    }

    public static void FromConfiguration(this SentryNLogOptions opts) => opts.FromConfiguration(KastaConfig.Instance);
    public static void FromConfiguration(this SentryNLogOptions opts, KastaConfig cfg)
    {
        var section = cfg.Sentry?.NLog;
        if (section == null) return;


        if (section.MinimumEventLevel.HasValue) opts.MinimumEventLevel = section.MinimumEventLevel.Value.Parse();
        if (section.MinimumBreadcrumbLevel.HasValue) opts.MinimumBreadcrumbLevel = section.MinimumBreadcrumbLevel.Value.Parse();

        opts.IgnoreEventsWithNoException = section.IgnoreEventsWithNoException;
        opts.IncludeEventPropertiesAsTags = section.IncludeEventPropertiesAsTags;
        opts.IncludeEventDataOnBreadcrumbs = section.IncludeEventDataOnBreadcrumbs;
    }

    public static SentryLevel Parse(this XmlSentryLevel input)
    {
        return input switch
        {
            XmlSentryLevel.Debug => SentryLevel.Debug,
            XmlSentryLevel.Info => SentryLevel.Info,
            XmlSentryLevel.Warning => SentryLevel.Warning,
            XmlSentryLevel.Error => SentryLevel.Error,
            XmlSentryLevel.Fatal => SentryLevel.Fatal,
            _ => throw new ArgumentException($"Cannot convert value \"{input}\" to {typeof(SentryLevel)}")
        };
    }

    public static NLog.LogLevel Parse(this XmlNLogLogLevel input)
    {
        return input switch
        {
            XmlNLogLogLevel.Trace => NLog.LogLevel.Trace,
            XmlNLogLogLevel.Debug => NLog.LogLevel.Debug,
            XmlNLogLogLevel.Info => NLog.LogLevel.Info,
            XmlNLogLogLevel.Warn => NLog.LogLevel.Warn,
            XmlNLogLogLevel.Error => NLog.LogLevel.Error,
            XmlNLogLogLevel.Fatal => NLog.LogLevel.Fatal,
            XmlNLogLogLevel.Off => NLog.LogLevel.Off,
            _ => throw new ArgumentException($"Cannot convert value \"{input}\" to {typeof(NLog.LogLevel)}")
        };
    }
}

