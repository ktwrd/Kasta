using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Kasta.Shared;

public class OAuthJwtElementItem
{
    [Required]
    [XmlAttribute("name")]
    public string InternalName { get; set; } = "";
    
    [Required]
    [XmlText]
    public string JwtValue { get; set; } = "";
}