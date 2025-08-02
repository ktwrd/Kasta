using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kasta.Data.Models;

public class S3FileChunkModel
{
    public const string TableName = "S3FileChunk";
    public S3FileChunkModel()
    {
        Id = Guid.NewGuid().ToString();
    }
    
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }

    [MaxLength(DatabaseHelper.GuidLength)]
    public string FileId { get; set; }
    
    public int ChunkIndex { get; set; }
    
    [MaxLength(200)]
    public string Sha256Hash { get; set; }

    [AuditIgnore]
    public S3FileInformationModel S3FileInformation { get; set; }
}