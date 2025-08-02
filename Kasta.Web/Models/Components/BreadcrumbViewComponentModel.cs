namespace Kasta.Web.Models.Components;

public class BreadcrumbViewComponentModel : List<BreadcrumbViewComponentItemModel>
{
}

public class BreadcrumbViewComponentItemModel
{
    public required string Text { get; set; }
    public string? TextIcon { get; set; }
    public TextDisplayKind TextDisplay { get; set; } = TextDisplayKind.Plain;
    public string? Link { get; set; }
    public bool Current { get; set; }

    public string ComputedClass
        => Current
        ? "breadcrumb-item active"
        : "breadcrumb-item";
}

