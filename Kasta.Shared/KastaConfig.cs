using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Xml.Serialization;

namespace Kasta.Shared;

[XmlRoot("Kasta")]
public class KastaConfig
{
    public static KastaConfig? Instance { get; set; }
    public static KastaConfig Get()
    {
        if (Instance != null)
        {
            return Instance;
        }
        var location = FeatureFlags.XmlConfigLocation;
        if (!File.Exists(location))
        {
            throw new InvalidOperationException($"Cannot get config since {location} doesn't exist (via {nameof(FeatureFlags)}.{nameof(FeatureFlags.XmlConfigLocation)})");
        }
        Instance = new();
        Instance.ReadFromFile(location);
        return Instance;
    }
    public void WriteToFile(string location)
    {
        using var file = new FileStream(location, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        file.SetLength(0);
        file.Seek(0, SeekOrigin.Begin);
        Write(file);
    }

    public void ReadFromFile(string location)
    {
        if (!File.Exists(location))
        {
            throw new ArgumentException($"{location} does not exist", nameof(location));
        }

        var content = File.ReadAllText(location);
        var xmlSerializer = new XmlSerializer(GetType());
        var xmlTextReader = new XmlTextReader(new StringReader(content)) {XmlResolver = null};
        var data = (KastaConfig?)xmlSerializer.Deserialize(xmlTextReader);
        if (data == null)
        {
            return;
        }

        foreach (var p in GetType().GetProperties())
        {
            p.SetValue(this, p.GetValue(data));
        }

        foreach (var f in GetType().GetFields())
        {
            f.SetValue(this, f.GetValue(data));
        }
    }

    public void Write(Stream stream)
    {
        var serializer = new XmlSerializer(GetType());
        var options = new XmlWriterSettings()
        {
            Indent = true
        };
        using var writer = XmlWriter.Create(stream, options);
        serializer.Serialize(writer, this);
    }

    [XmlElement("Auth")]
    public AuthConfigElement? Auth { get; set; }

    [XmlElement("Database")]
    public PostgreSQLConfigElement Database { get; set; } = new();

    [Required]
    [XmlElement("S3")]
    public S3ConfigElement S3 { get; set; } = new();

    [XmlElement("Kestrel")]
    public KestrelConfigElement? Kestrel { get; set; }

    [XmlElement("Sentry")]
    public SentryConfigElement? Sentry { get; set; }

    [DefaultValue("http://localhost:5280")]
    [XmlElement(nameof(Endpoint))]
    public string Endpoint { get; set; } = "http://localhost:5280";

    [DefaultValue("UTC")]
    [XmlElement(nameof(DefaultTimezone))]
    public string DefaultTimezone { get; set; } = "UTC";

    [XmlElement(nameof(Cache))]
    public CacheConfigElement Cache { get; set; } = new();
}