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
        var xml = new XmlSerializer(GetType());
        using var sww = new StringWriter();
        using (var wr = XmlWriter.Create(sww, new() {Indent = true}))
        {
            xml.Serialize(wr, this);
        }
        var content = sww.ToString();
        File.WriteAllText(location, content);
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

    [XmlElement("Auth")]
    public AuthConfigElement? Auth { get; set; }

    [XmlElement("Database")]
    public PostgreSQLConfigElement Database { get; set; } = new();

    [Required]
    [XmlElement("S3")]
    public S3ConfigElement S3 { get; set; } = new();

    [DefaultValue("http://localhost:5280")]
    [XmlElement(nameof(Endpoint))]
    public string Endpoint { get; set; } = "http://localhost:5280";

    [DefaultValue("UTC")]
    [XmlElement(nameof(DefaultTimezone))]
    public string DefaultTimezone { get; set; } = "UTC";

    [XmlElement(nameof(Cache))]
    public CacheConfigElement Cache { get; set; } = new();
}