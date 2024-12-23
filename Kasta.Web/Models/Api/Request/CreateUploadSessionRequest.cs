using System.ComponentModel.DataAnnotations;

namespace Kasta.Web.Models;

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