using System.ComponentModel.DataAnnotations;

namespace Kasta.Data.Models.Gallery;

public class GalleryTextHistoryModel
{
    public const string TableName = "GalleryTextHistory";
    public GalleryTextHistoryModel()
    {
        GalleryId = Guid.Empty.ToString();
        Timestamp = DateTime.UtcNow;
    }
    
    public string FakeId => $"{GalleryId}_{Timestamp.Ticks}";
    
    /// <summary>
    /// Foreign Key to <see cref="GalleryModel.Id"/>
    /// </summary>
    [MaxLength(DatabaseHelper.GuidLength)]
    public string GalleryId { get; set; }

    public GalleryModel Gallery { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    [MaxLength(200)]
    public string? Title { get; set; }
    
    [MaxLength(4000)]
    public string? Description { get; set; }
}