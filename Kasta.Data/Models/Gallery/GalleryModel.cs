using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kasta.Data.Models.Gallery;

public class GalleryModel
{
    public const string TableName = "Gallery";

    public GalleryModel()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        Public = true;
    }
    
    /// <summary>
    /// Primary Key (Guid)
    /// </summary>
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }

    [DefaultValue(true)]
    public bool Public { get; set; }
    
    [MaxLength(200)]
    public string? Title { get; set; }
    
    [MaxLength(4000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="UserModel.Id"/>
    /// </summary>
    [MaxLength(DatabaseHelper.GuidLength)]
    public string? CreatedByUserId { get; set; }
    public UserModel? CreatedByUser { get; set; }
    
    public List<GalleryFileAssociationModel> FileAssociations { get; set; }
}