using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Kasta.Shared;

public class OAuthJwtElement
{
    [Required]
    [XmlElement("Item")]
    public List<OAuthJwtElementItem> Items { get; set; } = [];
}