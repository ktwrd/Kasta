using Microsoft.AspNetCore.Mvc;
using Kasta.Web.Models.Components;

namespace Kasta.Web.ViewComponents;

public class FormCheckboxViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(FormCheckboxComponentViewModel data)
    {
        return View("Default", data);
    }
}