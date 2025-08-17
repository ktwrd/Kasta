using System.ComponentModel;
using Kasta.Data.Models;

namespace Kasta.Web.Models;

public class FileListViewModel
{
    public string? SearchQuery { get; set; }
    [DefaultValue(1)]
    public int Page { get; set; } = 1;
    public int TotalPageCount { get; set; } = 0;
    public bool IsLastPage { get; set; } = false;
    public List<FileModel> Files { get; set; } = [];

    public string? SpaceUsed { get; set; }
    public string? SpaceAvailable { get; set; }
}