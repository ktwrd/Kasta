using System.ComponentModel;

namespace Kasta.Web.Models.Components;

public class FormCheckboxComponentViewModel
{
    public required string Name { get; set; }
    public string? Id { get; set; }
    public string? ParentFormId { get; set; }
    public required string Label { get; set; }
    public bool State { get; set; }
    public string? ExtraClasses { get; set; }
    public string CalculatedClasses
    {
        get
        {
            string s = "form-check";
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
    public string CheckboxRole
    {
        get
        {
            switch (Kind)
            {
                case CheckboxKind.Switch:
                    return "switch";
                default:
                    return "";
            }
        }
    }
    [DefaultValue(CheckboxKind.Normal)]
    public CheckboxKind Kind { get; set; } = CheckboxKind.Normal;
    public bool Margin { get; set; } = false;
}
public enum CheckboxKind
{
    Normal,
    Switch
}