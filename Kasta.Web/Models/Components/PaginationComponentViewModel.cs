using System.ComponentModel;

namespace Kasta.Web.Models.Components;

public class PaginationComponentViewModel
{
    [DefaultValue(1)]
    public int Page { get; set; } = 1;
    public int TotalPageCount { get; set; } = 0;
    public bool IsLastPage { get; set; } = false;
    public required PaginationCreateUrlDelegate CreateUrlDelegate { get; set; }
}
public delegate string PaginationCreateUrlDelegate(int page);