using Kasta.Web.Models.Components;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.ViewComponents;

public class BreadcrumbViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(List<BreadcrumbViewComponentItemModel> model)
    {
        return Task.Run(IViewComponentResult () => View("Default", model));
    }
}