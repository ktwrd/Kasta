using System.ComponentModel;
using System.Xml.Serialization;

namespace Kasta.Shared.ConfigEditions;

[XmlRoot("Kasta")]
public class KastaConfig2025
{
    [XmlElement("Auth")]
    public AuthConfigElement? Auth { get; set; }

    [XmlElement("Database")]
    public PostgresDatabaseConfig Database { get; set; } = new();

    [XmlElement("S3")]
    public S3StorageConfig? S3 { get; set; } = new();

    [XmlElement("LocalFileStorage")]
    public LocalStorageConfig LocalFileStorage { get; set; } = new()
    {
        Enabled = true
    };

    [XmlElement("Kestrel")]
    public KestrelConfig? Kestrel { get; set; }

    [XmlElement("Sentry")]
    public SentryConfig? Sentry { get; set; }
    
    [XmlElement("Proxy")]
    public UpstreamProxyConfig? Proxy { get; set; }

    [DefaultValue("http://localhost:5280")]
    [XmlElement(nameof(Endpoint))]
    public string Endpoint { get; set; } = "http://localhost:5280";

    [DefaultValue("UTC")]
    [XmlElement(nameof(DefaultTimezone))]
    public string DefaultTimezone { get; set; } = "UTC";

    [XmlElement(nameof(Cache))]
    public CacheConfig Cache { get; set; } = new();
}