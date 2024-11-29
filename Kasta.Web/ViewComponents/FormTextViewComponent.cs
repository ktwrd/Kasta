using Microsoft.AspNetCore.Mvc;
using Kasta.Web.Models.Components;

namespace Kasta.Web.ViewComponents;

public class FormTextViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(FormTextComponentViewModel data)
    {
        return View("Default", data);
    }
}