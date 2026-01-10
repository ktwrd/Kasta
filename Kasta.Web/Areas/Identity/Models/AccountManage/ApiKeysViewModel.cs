using Kasta.Data.Models;
using Kasta.Web.Models.Components;

namespace Kasta.Web.Areas.Identity.Models.AccountManage;

public class ApiKeysViewModel
{
    public BaseAlertViewModel? Alert { get; set; }
    public List<UserApiKeyModel> ApiKeys { get; set; } = [];
    public required UserModel CurrentUser { get; set; }
}