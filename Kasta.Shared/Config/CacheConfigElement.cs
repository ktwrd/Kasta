using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Kasta.Shared;

public class CacheConfigElement
{
    [XmlElement("InMemory")]
    public InMemoryCacheElement? InMemory { get; set; }

    [XmlElement("Redis")]
    public RedisCacheElement? Redis { get; set; }

    /// <summary>
    /// Default value for <see cref="CachePrefix"/>. <c>EF_Kasta_</c>
    /// </summary>
    public const string CachePrefixDefault = "EF_Kasta_";

    [DefaultValue(CachePrefixDefault)]
    [XmlElement(nameof(CachePrefix))]
    public string CachePrefix { get; set; } = CachePrefixDefault;
}
public class InMemoryCacheElement
{
    [Required]
    [XmlAttribute(nameof(Enable))]
    public bool Enable { get; set; }

    [Required]
    [XmlAttribute(nameof(Name))]
    public string Name { get; set; } = "InMemory";

    [DefaultValue(120)]
    [XmlElement(nameof(MaxRandomSeconds))]
    public int MaxRandomSeconds { get; set; }

    [DefaultValue(true)]
    [XmlElement(nameof(EnableLogging))]
    public bool EnableLogging { get; set; }

    /// <summary>
    /// Default value for <see cref="LockMilliseconds"/>. <c>5000</c>
    /// </summary>
    public const int LockMillisecondsDefault = 5000;

    [DefaultValue(LockMillisecondsDefault)]
    [XmlElement(nameof(LockMilliseconds))]
    public int LockMilliseconds { get; set; } = LockMillisecondsDefault;

    /// <summary>
    /// Default value for <see cref="SleepMilliseconds"/>. <c>300</c>
    /// </summary>
    public const int SleepMillisecondsDefault = 300;
    [DefaultValue(SleepMillisecondsDefault)]
    [XmlElement(nameof(SleepMilliseconds))]
    public int SleepMilliseconds { get; set; } = SleepMillisecondsDefault;

    [XmlElement("DBConfig")]
    public InMemoryCacheDatabaseOptionsElement DbConfig { get; set; } = new();
    public class InMemoryCacheDatabaseOptionsElement
    {
        [DefaultValue(60)]
        [XmlElement(nameof(ExpirationScanFrequency))]
        public int ExpirationScanFrequency { get; set; }

        [DefaultValue(10000)]
        [XmlElement(nameof(SizeLimit))]
        public int SizeLimit { get; set; }

        [DefaultValue(true)]
        [XmlAttribute(nameof(EnableReadDeepClone))]
        public bool EnableReadDeepClone  { get; set;}

        [DefaultValue(false)]
        [XmlAttribute(nameof(EnableWriteDeepClone))]
        public bool EnableWriteDeepClone  { get; set; }
    }
}

public class RedisCacheElement
{
    [DefaultValue(true)]
    [XmlAttribute("Enable")]
    public bool Enable { get; set; }

    [DefaultValue(true)]
    [XmlElement(nameof(EnableLogging))]
    public bool EnableLogging { get; set; }

    [Required]
    [XmlElement("DBConfig")]
    public RedisCacheDatabaseOptionsElement DbConfig { get; set; } = new();

    public class RedisCacheEndpointElement
    {
        [Required]
        [XmlAttribute(nameof(Host))]
        public string Host { get; set; } = "";

        [Required]
        [XmlAttribute(nameof(Port))]
        public int Port { get; set; } = 6379;

        public RedisCacheEndpointElement()
            : this("127.0.0.1", 6379)
        { }

        public RedisCacheEndpointElement(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }

    public class RedisCacheDatabaseOptionsElement
    {
        #region RedisDBOptions
        [DefaultValue(0)]
        [XmlElement(nameof(Database))]
        public int Database { get; set; } = 0;

        [DefaultValue(10000)]
        [XmlElement(nameof(SyncTimeout))]
        public int SyncTimeout { get; set; } = 10000;

        [DefaultValue(10000)]
        [XmlElement(nameof(AsyncTimeout))]
        public int AsyncTimeout { get; set; } = 10000;
        #endregion

        #region BaseRedisOptions
        [DefaultValue(null)]
        [XmlElement(nameof(Username))]
        public string? Username { get; set; }

        [DefaultValue(null)]
        [XmlElement(nameof(Password))]
        public string? Password { get; set; }

        [DefaultValue(false)]
        [XmlElement(nameof(SslEnabled))]
        public bool SslEnabled { get; set; } = false;

        [DefaultValue(null)]
        [XmlElement(nameof(SslHost))]
        public string? SslHost { get; set; }

        /// <summary>
        /// Default value for <see cref="ConnectionTimeout"/>. <c>10000</c>
        /// </summary>
        public const int ConnectionTimeoutDefault = 10000;

        [DefaultValue(ConnectionTimeoutDefault)]
        [XmlElement(nameof(ConnectionTimeout))]
        public int ConnectionTimeout { get; set; } = ConnectionTimeoutDefault;

        [DefaultValue(false)]
        [XmlElement(nameof(AllowAdmin))]
        public bool AllowAdmin { get; set; } = false;

        [DefaultValue(true)]
        [XmlElement(nameof(AbortOnConnectFail))]
        public bool AbortOnConnectFail { get; set; } = true;

        [XmlElement("Endpoint")]
        public List<RedisCacheEndpointElement> Endpoints { get; set; } = [];
        #endregion
    }
}