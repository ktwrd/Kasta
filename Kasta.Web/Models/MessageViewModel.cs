using System.ComponentModel;

namespace Kasta.Web.Models;

public class MessageViewModel
{
    public string? Message { get; set; }

    /// <summary>
    /// Show message as an alert?
    /// Always <see langword="true"/> if <c>hx-</c> headers are set.
    /// </summary>
    [DefaultValue(false)]
    public bool IsAlert { get; set; }

    public bool MessageMarkdown { get; set; } = false;

    [DefaultValue(true)]
    public bool ShowHeader { get; set; } = true;

    public void Update(HttpContext context)
    {
        if (context.IsHtmxRequest()) IsAlert = true;
    }
}