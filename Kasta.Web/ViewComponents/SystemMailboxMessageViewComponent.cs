using Kasta.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.ViewComponents;

public class SystemMailboxMessageViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(SystemMailboxMessageModel model)
    {
        return Task.Run(IViewComponentResult () => View("Default", model));
    }
}