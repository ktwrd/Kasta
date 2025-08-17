using Kasta.Web.Models.Components;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.ViewComponents;

public class FormTextAreaViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(FormTextAreaComponentViewModel data)
    {
        return Task.Run(IViewComponentResult () => View("Default", data));
    }
}