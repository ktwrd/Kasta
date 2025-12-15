using System.ComponentModel;
using System.Xml.Serialization;

namespace Kasta.Shared;

public class LocalFileStorageConfigElement
{
    [XmlAttribute("Enabled")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [XmlAttribute("Directory")]
    public string? Directory { get; set; }
}