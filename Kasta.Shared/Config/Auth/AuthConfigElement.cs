using System.Xml.Serialization;

namespace Kasta.Shared;

public class AuthConfigElement
{
    [XmlElement("OAuth")]
    public List<GenericOAuthConfig> OAuth { get; set; } = [];

    public AuthStyleConfig? GetStyleForAuthId(string id)
    {
        return OAuth?.FirstOrDefault(e => e.Identifier == id && e.Style != null)?.Style;
    }
}