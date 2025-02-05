using Kasta.Data.Models;

namespace Kasta.Web.Models.Admin;

public class AdminUserListViewModel
{
    public List<UserModel> Users { get; set; } = [];
    public Dictionary<string, long> UserFileCount { get; set; } = [];
    public Dictionary<string, long> UserPreviewFileCount { get; set; } = [];
    public SystemSettingsParams SystemSettings { get; set; } = new();

    public int Page { get; set; }
    public bool IsLastPage { get; set; }
}