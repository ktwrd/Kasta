using Kasta.Data.Models;

namespace Kasta.Web.Models;

public class FileDetailViewModel
{
    public required FileModel File { get; set; }
    public string? PreviewContent { get; set; }
    public bool Embed { get; set; }
}