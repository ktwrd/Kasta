namespace Kasta.Web.Models.Components;

public class FormTextComponentViewModel
{
    public string? DisplayName { get; set; }
    public required string Name { get; set; }
    public required string Id { get; set; }

    public string? ParentFormId { get; set; }
    public string? Value { get; set; }
    public string? Placeholder { get; set; }
    public bool ReadOnly { get; set; }
    public string? ExtraClass { get; set; }
    public bool Required { get; set; }
    public bool Disabled { get; set; }
    public string? HelpText { get; set; }
}