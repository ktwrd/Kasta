using Kasta.Web.Models.Components;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.ViewComponents;

public class TextViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(TextViewComponentModel model)
    {
        return Task.Run(IViewComponentResult () => View("Default", model));
    }
}