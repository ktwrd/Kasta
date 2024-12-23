using Kasta.Data.Models;

namespace Kasta.Web.Models;

public class ProfileViewModel
{
    public required UserModel User { get; set; }
    public required UserSettingModel Settings { get; set; }

    public List<UserApiKeyModel> ApiKeys { get; set; } = [];
}