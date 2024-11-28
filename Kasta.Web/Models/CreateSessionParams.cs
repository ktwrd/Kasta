using System.ComponentModel.DataAnnotations;

namespace Kasta.Web.Models;

[Serializable]
public class CreateSessionParams
{
    [Required]
    public int? ChunkSize { get; set; }
    [Required]
    public long? TotalSize { get; set; }
    [Required]
    public string FileName { get; set; }
}