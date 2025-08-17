using Kasta.Data.Models.Gallery;

namespace Kasta.Web.Areas.Gallery.Models.Details;

public class DeleteConfirmViewModel
{
    public required int AffectedFiles { get; set; }
    public required GalleryModel Record { get; set; }
}