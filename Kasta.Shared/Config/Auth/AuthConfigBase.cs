using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Kasta.Shared;

public class AuthConfigBase
{
    /// <summary>
    /// Unique Identifier for OAuth. Required and should never be changed!
    /// </summary>
    [Required]
    [XmlAttribute("id")]
    public virtual string Identifier { get; set; } = "";
    
    /// <summary>
    /// Display Name for this authentication configuration item.
    /// Will be how it's displayed in the frontend.
    /// </summary>
    [Required]
    [XmlAttribute("DisplayName")]
    public string DisplayName { get; set; } = "";

    [XmlElement("Style")]
    public AuthStyleConfig? Style { get; set; }
}

/// <summary>
/// Style configuration for <see cref="AuthConfigBase"/>
/// </summary>
public class AuthStyleConfig
{
    [XmlElement("BackgroundColor")]
    public string? BackgroundColor { get; set; }

    [XmlElement("TextColor")]
    public string? TextColor { get; set; }

    [XmlAttribute("Class")]
    public string? Class
    {
        get;
        set => field = string.IsNullOrEmpty(value?.Trim()) ? null : value;
    }
}