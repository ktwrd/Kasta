using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace kate.FileShare.Data.Models;

public class FileModel
{
    public const string TableName = "File";
    public FileModel()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; set; }
    public string Filename { get; set; }
    public string RelativeLocation { get; set; }
    public string ShortUrl { get; set; }
    public string? MimeType { get; set; }
    public long Size { get; set; }

    public string? CreatedByUserId { get; set; }
    public UserModel? CreatedByUser { get; set; }
    
    public FilePreviewModel? Preview { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public S3FileInformationModel? S3FileInformation { get; set; }
}