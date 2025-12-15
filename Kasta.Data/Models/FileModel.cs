using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

namespace Kasta.Data.Models;

public class FileModel
{
    public const string TableName = "File";
    public FileModel()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }
    
    [MaxLength(1000)]
    public required string Filename { get; set; }
    
    [MaxLength(2000)]
    public required string RelativeLocation { get; set; }

    [MaxLength(100)]
    public string ShortUrl { get; set; }

    /// <summary>
    /// MIME Type of the file
    /// </summary>
    [MaxLength(150)]
    public string? MimeType { get; set; }
    
    /// <summary>
    /// File Size. Measured in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// When <see langword="false"/>, then only the creator can access this file.
    /// </summary>
    [DefaultValue(true)]
    public bool Public { get; set; } = true;

    [MaxLength(DatabaseHelper.GuidLength)]
    public string? CreatedByUserId { get; set; }
    
    [AuditIgnore]
    public UserModel? CreatedByUser { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    
    [AuditIgnore]
    public FilePreviewModel? Preview { get; set; }
    [AuditIgnore]
    public FileImageInfoModel? ImageInfo { get; set; }

    [AuditIgnore]
    public S3FileInformationModel? S3FileInformation { get; set; }
    
    [AuditIgnore]
    public NpgsqlTsVector SearchVector { get; set; }
}