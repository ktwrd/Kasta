using Kasta.Data.Models;
using Kasta.Web.Models.Components;

namespace Kasta.Web.Models;

public class LinkListViewModel
{
    public int Page { get; set; } = 1;
    public bool IsLastPage { get; set; } = false;
    public List<ShortLinkModel> Links { get; set; } = [];

    public LinkShortenerCreateModalModel CreateModalModel { get; set; } = new();
    public BaseAlertViewModel? Alert { get; set; }
    public BaseAlertViewModel? CreateModalAlert { get; set; }
    public bool OpenCreateModal { get; set; }
}