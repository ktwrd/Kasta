namespace Kasta.Web.Models.Components;

public class FormTextComponentViewModel
{
    /// <summary>
    /// When not <see langword="null"/> or empty, this will be displayed before the input element using a label with the <c>form-label</c> class.
    /// </summary>
    public string? DisplayName { get; set; }
    /// <summary>
    /// Value for the <c>name</c> attribute on the input element.
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Value for the <c>id</c> attribute on the input element.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Value for the <c>form</c> attribute on the input element.
    /// </summary>
    public string? ParentFormId { get; set; }
    /// <summary>
    /// Value for the <c>value</c> attribute on the input element.
    /// </summary>
    public string? Value { get; set; }
    /// <summary>
    /// Value for the <c>placeholder</c> attribute on the input element.
    /// </summary>
    public string? Placeholder { get; set; }
    /// <summary>
    /// When <see langword="true"/>, the <c>readonly</c> attribute will be put on the element.
    /// </summary>
    public bool ReadOnly { get; set; }
    /// <summary>
    /// Extra stuff to append to the <c>class</c> attribute on the input element. Will be ignored when <see langword="null"/> or empty.
    /// </summary>
    public string? ExtraClass { get; set; }
    /// <summary>
    /// When <see langword="true"/>, the <c>required</c> attribute will be put on the element.
    /// </summary>
    public bool Required { get; set; }
    /// <summary>
    /// When <see langword="true"/>, the <c>disabled</c> attribute will be put on the element.
    /// </summary>
    public bool Disabled { get; set; }
    /// <summary>
    /// Help text to display below the text input. Will be parsed as markdown.
    /// </summary>
    public string? HelpText { get; set; }
}