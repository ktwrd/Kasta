using Kasta.Web.Models.Components;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.ViewComponents;

public class PaginationViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(PaginationComponentViewModel model)
    {
        return Task.Run(IViewComponentResult () => View("Default", model));
    }
}