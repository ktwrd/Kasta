using System.ComponentModel;

namespace Kasta.Web.Models.Components;

public class FormCheckboxComponentViewModel
{
    /// <summary>
    /// Value for the <c>name</c> attribute on the input element.
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Value for the <c>id</c> attribute on the input element.
    /// </summary>
    public string? Id { get; set; }
    /// <summary>
    /// Value for the <c>form</c> attribute on the input element.
    /// </summary>
    public string? ParentFormId { get; set; }
    /// <summary>
    /// Label for the checkbox.
    /// </summary>
    public required string Label { get; set; }
    /// <summary>
    /// Is the checkbox checked or not?
    /// </summary>
    public bool State { get; set; }
    /// <summary>
    /// Extra stuff to append to the <c>class</c> attribute on the input element. Will be ignored when <see langword="null"/> or empty.
    /// </summary>
    public string? ExtraClasses { get; set; }
    /// <summary>
    /// Computed value for the <c>class</c> attribute on the div that encases the checkbox input.
    /// </summary>
    public string CalculatedClasses
    {
        get
        {
            var s = "form-check";
            if (Margin)
            {
                s += " mb-3";
            }
            if (string.IsNullOrEmpty(ExtraClasses) == false)
            {
                s += " " + ExtraClasses;
            }
            return s;
        }
    }
    public string? HelpText { get; set; }
    public string CheckboxRole => Kind switch
    {
        CheckboxKind.Switch => "switch",
        _ => ""
    };
    [DefaultValue(CheckboxKind.Normal)]
    public CheckboxKind Kind { get; set; } = CheckboxKind.Normal;
    /// <summary>
    /// <see langword="true"/> when a margin should be added to the bottom of the checkbox.
    /// </summary>
    public bool Margin { get; set; } = false;
}
public enum CheckboxKind
{
    Normal,
    Switch
}