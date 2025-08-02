using System.ComponentModel.DataAnnotations;

namespace Kasta.Web.Models.Api.Request;

[Serializable]
public class CreateUploadSessionRequest
{
    [Required]
    public int? ChunkSize { get; set; }

    [Required]
    public long? TotalSize { get; set; }

    [Required]
    public string Filename { get; set; } = "";
}