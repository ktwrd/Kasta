using Microsoft.AspNetCore.Mvc;
using Kasta.Web.Models.Components;

namespace Kasta.Web.ViewComponents;

public class FormTextViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(FormTextComponentViewModel data)
    {
        return Task.Run(() => (IViewComponentResult)View("Default", data));
    }
}