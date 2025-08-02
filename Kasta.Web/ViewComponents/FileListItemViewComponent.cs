using Kasta.Data.Models;
using Kasta.Web.Models.Components;
using Microsoft.AspNetCore.Mvc;

namespace Kasta.Web.ViewComponents;

public class FileListItemViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(FileListItemComponentViewModel file)
    {
        return Task.Run(IViewComponentResult () => View("Default", file));
    }
}