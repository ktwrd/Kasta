using System.ComponentModel.DataAnnotations.Schema;

namespace kate.FileShare.Data.Models;

public class ChunkUploadSessionModel
{
    public const string TableName = "ChunkUploadSession";
    public ChunkUploadSessionModel()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; set; }

    [ForeignKey(nameof(User))]
    public string? UserId { get; set; }
    public UserModel? User { get; set; }

    [ForeignKey(nameof(File))]
    public string FileId { get; set; }
    public FileModel File { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}