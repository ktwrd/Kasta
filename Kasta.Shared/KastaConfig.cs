using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Xml.Serialization;

namespace Kasta.Shared;

[XmlRoot("Kasta")]
public class KastaConfig
{
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
    public KastaAuthConfig? Auth { get; set; }

    [XmlElement("Database")]
    public KastaDatabaseConfigElement Database { get; set; } = new();

    [Required]
    [XmlElement("S3")]
    public KastaS3Config S3 { get; set; } = new();

    [DefaultValue("http://localhost:5280")]
    [XmlElement(nameof(Endpoint))]
    public string Endpoint { get; set; } = "http://localhost:5280";

    [DefaultValue("UTC")]
    [XmlElement(nameof(DefaultTimezone))]
    public string DefaultTimezone { get; set; } = "UTC";
}

public class KastaS3Config
{
    [Required]
    [XmlElement(nameof(ServiceUrl))]
    public string ServiceUrl { get; set; } = "";

    [Required]
    [XmlElement(nameof(AccessKey))]
    public string AccessKey { get; set; } = "";

    [Required]
    [XmlElement(nameof(AccessSecret))]
    public string AccessSecret { get; set; } = "";
    
    [Required]
    [XmlElement(nameof(BucketName))]
    public string BucketName { get; set; } = "";

    
    [XmlAttribute("ForcePathStyle")]
    [DefaultValue(false)]
    public bool ForcePathStyle { get; set; } = false;
}

public class KastaAuthConfig
{
    [XmlElement("OAuth")]
    public List<KastaOAuthConfigElement> OAuth { get; set; } = [];
}

public class KastaDatabaseConfigElement
{
    [DefaultValue("postgres")]
    [XmlAttribute("Host")]
    public string Hostname { get; set; } = "postgres";

    [DefaultValue(5432)]
    [XmlAttribute("Port")]
    public int Port { get; set; } = 5432;

    [DefaultValue("kasta")]
    [XmlAttribute("Name")]
    public string Name { get; set; } = "kasta";

    [DefaultValue("postgres")]
    [XmlElement("Username")]
    public string Username { get; set; } = "postgres";
    [DefaultValue("")]
    [XmlElement("Password")]
    public string Password { get; set; } = "";
}

public class KastaOAuthConfigElement
{
    [Required]
    [XmlAttribute("id")]
    public string Identifier { get; set; } = "";

    [Required]
    [XmlAttribute("DisplayName")]
    public string DisplayName { get; set; } = "";

    [XmlAttribute("Enable")]
    [DefaultValue(true)]
    public bool Enabled { get; set; }

    [Required]
    [XmlElement(nameof(ClientId))]
    public string ClientId { get; set; } = "";

    [Required]
    [XmlElement(nameof(ClientSecret))]
    public string ClientSecret { get; set; } = "";

    [Required]
    [XmlElement(nameof(Endpoint))]
    public string Endpoint { get; set; } = "";

    [Required]
    [XmlElement(nameof(ValidateIssuer))]
    public bool ValidateIssuer { get; set; }

    [Required]
    [XmlElement("Scope")]
    public List<string> Scopes { get; set; } = [];

    [XmlElement("Jwt")]
    public KastaOAuthJwtElement? Jwt { get; set; }
}
public class KastaOAuthJwtElement
{
    [Required]
    [XmlElement("Item")]
    public List<KastaOAuthJwtItem> Items { get; set; } = [];
}
public class KastaOAuthJwtItem
{
    [Required]
    [XmlAttribute("name")]
    public string InternalName { get; set; } = "";
    
    [Required]
    [XmlText]
    public string JwtValue { get; set; } = "";
}