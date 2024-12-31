using System.ComponentModel;
using System.Xml.Serialization;

namespace Kasta.Shared;

public class PostgreSQLConfigElement
{
    [DefaultValue("postgres")]
    [XmlAttribute("Host")]
    public string Host { get; set; } = "postgres";

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