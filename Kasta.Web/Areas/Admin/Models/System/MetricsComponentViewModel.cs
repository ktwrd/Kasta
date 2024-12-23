using Kasta.Web.Models;

namespace Kasta.Web.Areas.Admin.Models.System;

public class MetricsComponentViewModel : BaseAlertViewModel, IMetricsComponentViewModel
{
    public int UserCount { get; set; }
    public int FileCount { get; set; }
    public int OrphanFileCount { get; set; }
    public string TotalSpaceUsed { get; set; } = "0B";
    public string TotalPreviewSpaceUsed { get; set; } = "0B";
    public int LinkCount { get; set; }
}
public interface IMetricsComponentViewModel
{
    public int UserCount { get; set; }
    public int FileCount { get; set; }
    public int OrphanFileCount { get; set; }
    public string TotalSpaceUsed { get; set; }
    public string TotalPreviewSpaceUsed { get; set; }
    public int LinkCount { get; set; }
}