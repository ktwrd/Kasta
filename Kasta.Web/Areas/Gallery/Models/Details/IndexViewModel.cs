using Kasta.Data.Models;
using Kasta.Data.Models.Gallery;

namespace Kasta.Web.Areas.Gallery.Models.Details;

public class IndexViewModel
{
    /// <summary>
    /// Includes: <see cref="FileModel.Preview"/>, and <see cref="FileModel.ImageInfo"/>
    /// </summary>
    public List<FileModel> Files { get; set; } = [];
    /// <summary>
    /// Includes: <see cref="GalleryModel.CreatedByUser"/>
    /// </summary>
    public GalleryModel Gallery { get; set; }
    public UserModel? Author { get; set; }
    public required UserSettingModel UserSettings { get; set; }
    
    public bool CanEdit { get; set; } = false;
}