using System.ComponentModel;
using Microsoft.AspNetCore.Identity;

namespace kate.FileShare.Data.Models;

public class UserModel : IdentityUser
{
    public const string TableName = "AspNetUsers";
    public UserLimitModel? Limit { get; set; }

    [DefaultValue(false)]
    public bool IsAdmin { get; set; }
}