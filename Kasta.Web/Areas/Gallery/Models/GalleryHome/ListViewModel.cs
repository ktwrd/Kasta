using System.ComponentModel;
using Kasta.Data.Models.Gallery;

namespace Kasta.Web.Areas.Gallery.Models.GalleryHome;

public class ListViewModel
{
    public List<GalleryModel> Galleries { get; set; } = [];
    [DefaultValue(1)]
    public int Page { get; set; } = 1;
    public int TotalPageCount { get; set; } = 0;
    public bool IsLastPage { get; set; } = false;
    
    public string? SpaceUsed { get; set; }
    public string? SpaceAvailable { get; set; }
}