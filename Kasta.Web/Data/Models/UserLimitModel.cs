using System.ComponentModel;
using Microsoft.AspNetCore.Identity;

namespace Kasta.Web.Data.Models;

public class UserLimitModel
{
    public const string TableName = "UserLimits";
    public string UserId { get; set; }
    public UserModel User { get; set; }
    public long? MaxFileSize { get; set; }
    public long? MaxStorage { get; set; }

    [DefaultValue(0)]
    public long SpaceUsed { get; set; }
}