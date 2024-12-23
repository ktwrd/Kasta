using Microsoft.AspNetCore.Mvc;
using Kasta.Web.Models.Components;

namespace Kasta.Web.ViewComponents;

public class FormCheckboxViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(FormCheckboxComponentViewModel data)
    {
        return Task.Run(() => {
            return (IViewComponentResult)View("Default", data);
        });
    }
}