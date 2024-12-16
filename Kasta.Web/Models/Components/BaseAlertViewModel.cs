using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Kasta.Web.Models;

public class BaseAlertViewModel
{
    public string? AlertType { get; set; }
    public string? AlertClass
    {
        get
        {
            if (string.IsNullOrEmpty(AlertType))
                return null;
            
            var t = AlertType.Trim().ToLower();
            var list = new List<string>()
            {
                "alert"
            };
            if (ValidAlertTypes.Contains(t))
            {
                list.Add($"alert-{t}");
            }
            else
            {
                list.Add("alert-secondary");
            }

            if (ShowAlertCloseButton)
            {
                list.Add("alert-dismissible");
            }

            return string.Join(" ", list);
        }
    }
    private static ReadOnlyCollection<string> ValidAlertTypes => new List<string>()
    {
        "primary",
        "secondary",
        "success",
        "danger",
        "warning",
        "info"
    }.AsReadOnly();
    public string? AlertContent { get; set; }
    [DefaultValue(true)]
    public bool ShowAlertCloseButton { get; set; } = true;
    [DefaultValue(true)]

    public bool AlertContentAsMarkdown { get; set; } = true;
}