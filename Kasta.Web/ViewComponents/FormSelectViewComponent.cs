using Microsoft.AspNetCore.Mvc;
using Kasta.Web.Models.Components;

namespace Kasta.Web.ViewComponents;

public class FormSelectViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(FormSelectComponentViewModel data)
    {
        return Task.Run(() => (IViewComponentResult)View("Default", data));
    }
}