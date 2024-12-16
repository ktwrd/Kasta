using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Kasta.Data.Models;

public class UserModel : IdentityUser
{
    public const string TableName = "AspNetUsers";
    
    [AuditIgnore]
    public UserLimitModel? Limit { get; set; }

    [DefaultValue(false)]
    public bool IsAdmin { get; set; }

    [MaxLength(100)]
    public string? ThemeName { get; set; }

    [AuditIgnore]
    public UserSettingModel? Settings { get; set; }
    
    [AuditIgnore]
    public List<UserApiKeyModel> ApiKeys { get; set; } = [];
}