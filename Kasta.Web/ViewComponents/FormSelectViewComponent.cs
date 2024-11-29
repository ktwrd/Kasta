using Microsoft.AspNetCore.Mvc;
using Kasta.Web.Models.Components;

namespace Kasta.Web.ViewComponents;

public class FormSelectViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(FormSelectComponentViewModel data)
    {
        return View("Default", data);
    }
}