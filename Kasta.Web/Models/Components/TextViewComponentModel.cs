using System.ComponentModel;

namespace Kasta.Web.Models.Components;

public class TextViewComponentModel
{
    public string? Content { get; set; }
    
    [DefaultValue(TextDisplayKind.Plain)]
    public TextDisplayKind Kind { get; set; } = TextDisplayKind.Plain;
    
    public string? CodeBlockClass { get; set; }
}
public enum TextDisplayKind
{
    Plain,
    Html,
    Markdown,
    MarkdownBasic,
    CodeBlock
}