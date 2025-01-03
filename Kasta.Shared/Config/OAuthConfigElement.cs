using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Kasta.Shared;

public class OAuthConfigElement
{
    /// <summary>
    /// Unique Identifier for OAuth. Required and should never be changed!
    /// </summary>
    [Required]
    [XmlAttribute("id")]
    public string Identifier { get; set; } = "";

    /// <summary>
    /// Display Name for this OAuth configuration item. Will be how it's displayed in the frontend.
    /// </summary>
    [Required]
    [XmlAttribute("DisplayName")]
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Will be ignored when <see langword="false"/>
    /// </summary>
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

    /// <summary>
    /// Validate the issuer certificate 'n such
    /// </summary>
    [Required]
    [XmlElement(nameof(ValidateIssuer))]
    public bool ValidateIssuer { get; set; }

    [Required]
    [XmlElement("Scope")]
    public List<string> Scopes { get; set; } = [];

    [XmlElement("Jwt")]
    public OAuthJwtElement? Jwt { get; set; }
}