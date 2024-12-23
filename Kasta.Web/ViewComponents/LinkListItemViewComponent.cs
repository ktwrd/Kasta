using Kasta.Web.Models.Components;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.ViewComponents;

public class LinkListItemViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(LinkListItemComponentViewModel link)
    {
        return Task.Run(() => (IViewComponentResult)View("Default", link));
    }
}