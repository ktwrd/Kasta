using Kasta.Web.Models.Components;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.ViewComponents;

public class AdminUserListItemViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(AdminUserListItemViewComponentModel model)
    {
        return Task.Run(IViewComponentResult () => View("Default", model));
    }
}