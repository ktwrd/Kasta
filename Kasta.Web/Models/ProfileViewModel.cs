using Kasta.Data.Models;

namespace Kasta.Web.Models;

public class ProfileViewModel
{
    public UserModel User { get; set; }
    public UserSettingModel Settings { get; set; }
}