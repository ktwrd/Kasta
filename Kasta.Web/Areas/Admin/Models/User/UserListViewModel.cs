using Kasta.Data.Models;
using Kasta.Web.Models;

namespace Kasta.Web.Areas.Admin.Models.User;

public class UserListViewModel
{
    public List<UserModel> Users { get; set; } = [];
    public Dictionary<string, long> UserFileCount { get; set; } = [];
    public Dictionary<string, long> UserPreviewFileCount { get; set; } = [];
    public SystemSettingsParams SystemSettings { get; set; } = new();

    public int Page { get; set; }
    public bool IsLastPage { get; set; }
}