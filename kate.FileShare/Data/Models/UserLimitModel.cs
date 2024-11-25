using System.ComponentModel;
using Microsoft.AspNetCore.Identity;

namespace kate.FileShare.Data.Models;

public class UserLimitModel
{
    public const string TableName = "UserLimits";
    public string UserId { get; set; }
    public UserModel User { get; set; }
    public int? MaxFileSize { get; set; }
    public int? MaxStorage { get; set; }

    [DefaultValue(0)]
    public long SpaceUsed { get; set; }
}