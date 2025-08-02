using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kasta.Data.Models;

public class FilePreviewModel
{
    public const string TableName = "FilePreview";
    public FilePreviewModel()
    {
        Id = Guid.Empty.ToString();
        CreatedAt = DateTimeOffset.UtcNow;
    }
    
    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }

    [AuditIgnore]
    public FileModel File { get; set; }
    
    /// <summary>
    /// Size of the preview file (measured in bytes)
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Relative location in the S3 bucket
    /// </summary>
    [MaxLength(500)]
    public string RelativeLocation { get; set; }
    
    [MaxLength(250)]
    public string Filename { get; set; }

    /// <summary>
    /// Mime type of the file. Should be an image of some sort.
    /// </summary>
    [MaxLength(150)]
    public string MimeType { get; set; }

    /// <summary>
    /// When the preview was created at
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}