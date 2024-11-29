using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Kasta.Data.Models;

public class UserModel : IdentityUser
{
    public const string TableName = "AspNetUsers";
    public UserLimitModel? Limit { get; set; }

    [DefaultValue(false)]
    public bool IsAdmin { get; set; }

    [MaxLength(100)]
    public string? ThemeName { get; set; }

    public UserSettingModel? Settings { get; set; }
    public List<UserApiKeyModel> ApiKeys { get; set; } = [];
}