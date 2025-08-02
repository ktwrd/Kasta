using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kasta.Data.Models;

public class ChunkUploadSessionModel
{
    public const string TableName = "ChunkUploadSession";
    public ChunkUploadSessionModel()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTimeOffset.UtcNow;
    }
    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }

    [MaxLength(DatabaseHelper.GuidLength)]
    public string? UserId { get; set; }

    [AuditIgnore]
    public UserModel? User { get; set; }

    [MaxLength(DatabaseHelper.GuidLength)]
    public string FileId { get; set; }

    [AuditIgnore]
    public FileModel File { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}