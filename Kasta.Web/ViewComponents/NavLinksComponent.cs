using Kasta.Web.Models.Components;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.ViewComponents;

public class NavLinksViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(NavLinksComponentViewModel link)
    {
        return Task.Run(IViewComponentResult () => View("Default", link));
    }
}