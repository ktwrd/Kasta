using Kasta.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.ViewComponents;

public class AlertViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(BaseAlertViewModel model)
    {
        return Task.Run(() => (IViewComponentResult)View("Default", model));
    }
}