using System.ComponentModel.DataAnnotations;

namespace Kasta.Data.Models.Gallery;

public class GalleryFileAssociationModel
{
    public const string TableName = "GalleryFileAssociation";

    public GalleryFileAssociationModel()
    {
        GalleryId = Guid.Empty.ToString();
        FileId = Guid.Empty.ToString();
    }
    
    /// <summary>
    /// Foreign Key to <see cref="GalleryModel.Id"/>
    /// </summary>
    [MaxLength(DatabaseHelper.GuidLength)]
    public string GalleryId { get; set; }
    
    /// <summary>
    /// Accessor for <see cref="GalleryId"/>
    /// </summary>
    public GalleryModel Gallery { get; set; }
    
    /// <summary>
    /// Foreign Key to <see cref="FileModel.Id"/>
    /// </summary>
    [MaxLength(DatabaseHelper.GuidLength)]
    public string FileId { get; set; }
    
    public FileModel File { get; set; }
    
    public int SortOrder { get; set; }
}