using System.Xml.Serialization;

namespace Kasta.Shared;

public class AuthConfigElement
{
    [XmlElement("OAuth")]
    public List<GenericOAuthConfig> OAuth { get; set; } = [];
}