using System.Xml.Serialization;

namespace Kasta.Shared;

public class SentryConfig
{
    /// <summary>
    /// The rate to sample error and crash events.
    /// </summary>
    /// <remarks>
    /// Can be anything between 0.01 (1%) and 1.0 (99.9%) or &lt; 0.00 (default), to disable it.
    /// When greater than 1.0, it'll be clamped (3.5 is now 0.5, etc...)
    /// </remarks>
    [XmlElement("SampleRate")]
    public float? SampleRate { get; set; }

    /// <summary>
    /// <para>The sampling rate for profiling is relative to <see cref="Sentry.SentryOptions.TracesSampleRate"/>.
    /// Setting to 1.0 will profile 100% of sampled transactions. </para>
    /// 
    /// Value – Effect
    /// <para>&gt;= 0.0 and &lt;=1.0 – A custom sample rate is. Values outside of this range are
    /// invalid. Setting to 0.0 will disable profiling.</para>
    /// <para>&lt; 0.0 The default setting. At this time, this is equivalent to 0.0, i.e. disabling
    /// profiling, but that may change in the future.</para>
    /// </summary>
    [XmlElement("ProfilesSampleRate")]
    public double? ProfilesSampleRate { get; set; }

    /// <summary>
    /// <para>Indicates the percentage of the tracing data that is collected.</para>
    /// 
    /// Value – Effect
    /// <para>&gt;= 0.0 and &lt;=1.0 – A custom sample rate is used unless overriden by a <see cref="Sentry.SentryOptions.TracesSampler"/>
    /// function. Values outside of this range are invalid.</para>
    /// <para>&lt; 0.0 – The default setting. The tracing sample rate is determined by the <see cref="Sentry.SentryOptions.TracesSampler"/>
    /// function.</para>
    /// </summary>
    /// <remarks>
    /// Random sampling rate is only applied to transactions that don't already have
    /// a sampling decision set by other means, such as through <see cref="Sentry.SentryOptions.TracesSampler"/>,
    /// by inheriting it from an incoming trace header, or by copying it from <see cref="Sentry.TransactionContext"/>.
    /// </remarks>
    [XmlElement("TracesSampleRate")]
    public double? TracesSampleRate { get; set; }

    [XmlElement("DiagnosticLevel")]
    public XmlSentryLevel? DiagnosticLevel { get; set; }

    /// <summary>
    /// The level of the event sent to Sentry.
    /// </summary>
    public enum XmlSentryLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    /// <summary>
    /// NLog Sentry configuration
    /// </summary>
    [XmlElement("NLog")]
    public SentryNLogConfigElement? NLog { get; set; }
}

public class SentryNLogConfigElement
{
    [XmlElement("MinimumEventLevel")]
    public XmlLogLevel? MinimumEventLevel { get; set; }

    [XmlElement("MinimumBreadcrumbLevel")]
    public XmlLogLevel? MinimumBreadcrumbLevel { get; set; }

    [XmlElement("IgnoreEventsWithNoException")]
    public bool IgnoreEventsWithNoException { get; set; } = false;

    [XmlElement("IncludeEventPropertiesAsTags")]
    public bool IncludeEventPropertiesAsTags { get; set; } = false;

    [XmlElement("IncludeEventDataOnBreadcrumbs")]
    public bool IncludeEventDataOnBreadcrumbs { get; set; } = false;

    public enum XmlLogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Fatal = 5,
        Off = 6
    }
}