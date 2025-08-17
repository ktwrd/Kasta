using Kasta.Data.Models;

namespace Kasta.Web.Models.Components;

public class AdminUserListItemViewComponentModel
{
    public required UserModel User { get; set; }
    public required SystemSettingsParams SystemSettings { get; set; }
    public Dictionary<string, long> UserFileCount { get; set; } = [];
    public Dictionary<string, long> UserPreviewFileCount { get; set; } = [];
}