namespace Kasta.Web.Models.Admin;

public class AdminIndexViewModel
{
    public SystemSettingsParams SystemSettings { get; set; } = new();

    public int UserCount { get; set; }
    public int FileCount { get; set; }
    public int OrphanFileCount { get; set; }
    public string TotalSpaceUsed { get; set; } = "0B";
    public string TotalPreviewSpaceUsed { get; set; } = "0B";
}