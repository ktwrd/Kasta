namespace Kasta.Web.Models.Components;

public class FormSelectComponentViewModel
{
    /// <summary>
    /// <c>form</c> attribute for the <c>select</c> element.
    /// </summary>
    public string? ParentFormId { get; set; }
    /// <summary>
    /// <c>name</c> attribute on the <c>select</c> element.
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// <c>id</c> attribute for the <c>select</c> element, and the <c>for</c> attribute on the <c>label</c> element if it exists.
    /// </summary>
    public required string Id { get; set; }
    /// <summary>
    /// When <see langword="null"/> no label will be generated.
    /// </summary>
    public string? Label { get; set; }
    /// <summary>
    /// Help text to display below the select control. Will be parsed as markdown.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// When <see langword="true"/> the <c> multiple</c> attribute will be put on the <c>select</c> element.
    /// </summary>
    /// <remarks>
    /// When setting the value for <see cref="SelectedValues"/>, only the first value will be respected.
    /// </remarks>
    public bool Multiple { get; set; }
    public bool Required { get; set; }
    public bool Disabled { get; set; }

    public List<FormSelectItem> Items { get; set; } = [];

    private List<object> _selectedValues = [];

    /// <summary>
    /// Value for the selected item in <see cref="Items"/>. Will only be used when <see cref="Multiple"/> is set to <see langword="false"/>
    /// </summary>
    public object? SelectedValue
    {
        get
        {
            lock (_selectedValues)
            {
                return _selectedValues.FirstOrDefault();
            }
        }
        set
        {
            lock (_selectedValues)
            {
                _selectedValues.Clear();
                if (value != null)
                {
                    _selectedValues.Add(value);
                }
            }
        }
    }
    public IEnumerable<object> SelectedValues
    {
        get
        {
            lock (_selectedValues)
            {
                if (Multiple)
                {
                    return _selectedValues.ToList();
                }
                
                var r = new List<object>();
                if (_selectedValues.Count > 0)
                {
                    r.Add(_selectedValues.First());
                }
                return r;
            }
        }
        set
        {
            lock (Items)
            {
                var r = new List<object>();
                foreach (var x in Items.Where(e => value.Contains(e.Value)))
                {
                    r.Add(x.Value);
                    if (!Multiple) break;
                }
                _selectedValues = r;
            }
        }
    }

    public FormSelectItem AddItem(string label, object value, bool disabled = false)
    {
        var x = new FormSelectItem()
        {
            Value = value,
            Label = label,
            Disabled = disabled
        };
        Items.Add(x);
        return x;
    }
}
public class FormSelectItem
{
    public required object Value { get; set; }
    public required string Label { get; set; }
    public bool Disabled { get; set; }
}