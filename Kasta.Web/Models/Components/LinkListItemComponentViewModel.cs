using Kasta.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.Models.Components;

public class LinkListItemComponentViewModel
{
    public required ShortLinkModel Link { get; set; }

    public virtual string DeleteUrl(IUrlHelper url, ShortLinkModel link, string targetReturnUrl)
    {
        return GenerateAction(url, "DeleteShortenedLink", "Home", new()
        {
            {"value", link.Id},
            {"returnUrl", targetReturnUrl}
        });
    }
    public virtual string GetUrl(ShortLinkModel link)
    {
        return $"/l/" + (string.IsNullOrEmpty(link.ShortLink) ? link.Id : link.ShortLink);
    }
    public string GenerateAction(IUrlHelper url, string action, string controller, Dictionary<string, object> parameters)
    {
        var u = url.Action(action, controller, parameters);
        if (string.IsNullOrEmpty(u))
        {
            var j = string.Join(", ", parameters.Select(e => $"{e.Key}, {e.Value}"));
            var js = string.IsNullOrEmpty(j) ? "" : $" ({j})";
            throw new InvalidDataException($"Failed to generate Action Url for {nameof(controller)}={controller}, {nameof(action)}={action}" + js);
        }
        return u;
    }
}