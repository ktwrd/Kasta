using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    public string Filename { get; set; }
    public string RelativeLocation { get; set; }
    public string ShortUrl { get; set; }
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