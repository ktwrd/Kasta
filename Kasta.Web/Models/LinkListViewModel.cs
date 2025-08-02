using Kasta.Data.Models;

namespace Kasta.Web.Models;

public class LinkListViewModel
{
    public int Page { get; set; } = 1;
    public bool IsLastPage { get; set; } = false;
    public List<ShortLinkModel> Links { get; set; } = [];
}