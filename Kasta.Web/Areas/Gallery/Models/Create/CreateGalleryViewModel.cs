using Kasta.Data.Models.Gallery;
using Kasta.Web.Models.Components;

namespace Kasta.Web.Areas.Gallery.Models.Create;

public class CreateGalleryViewModel
{
    public required GalleryModel Gallery { get; set; }
    
    public BaseAlertViewModel? Alert { get; set; }
}