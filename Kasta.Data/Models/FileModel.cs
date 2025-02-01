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
    public long Size { get; set; }

    [DefaultValue(true)]
    public bool Public { get; set; } = true;

    public string? CreatedByUserId { get; set; }
    public UserModel? CreatedByUser { get; set; }
    
    [AuditIgnore]
    public FilePreviewModel? Preview { get; set; }
    [AuditIgnore]
    public FileImageInfoModel? ImageInfo { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [AuditIgnore]
    public S3FileInformationModel? S3FileInformation { get; set; }
    
    [AuditIgnore]
    public NpgsqlTsVector SearchVector { get; set; }
}