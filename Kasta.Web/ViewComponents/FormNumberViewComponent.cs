using Microsoft.AspNetCore.Mvc;
using Kasta.Web.Models.Components;

namespace Kasta.Web.ViewComponents;

public class FormNumberViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(FormNumberComponentViewModel data)
    {
        return Task.Run(IViewComponentResult () => View("Default", data));
    }
}