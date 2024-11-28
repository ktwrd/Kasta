using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kasta.Web.Data.Models;

public class S3FileChunkModel
{
    public const string TableName = "S3FileChunk";
    public S3FileChunkModel()
    {
        Id = Guid.NewGuid().ToString();
    }
    public string Id { get; set; }

    public string FileId { get; set; }
    public int ChunkIndex { get; set; }
    public string Sha256Hash { get; set; }

    public S3FileInformationModel S3FileInformation { get; set; }
}