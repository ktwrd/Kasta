using Kasta.Data.Models;

namespace Kasta.Web.Models;

public class FileListViewModel
{
    public string? SearchQuery { get; set; }
    public int Page { get; set; } = 1;
    public bool IsLastPage {get;set;} = false;
    public List<FileModel> Files { get; set; } = [];

    public string? SpaceUsed { get; set; }
    public string? SpaceAvailable { get; set; }
}