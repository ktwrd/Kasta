using System.ComponentModel;
using Microsoft.AspNetCore.Identity;

namespace Kasta.Data.Models;

public class UserLimitModel
{
    public const string TableName = "UserLimits";
    public UserLimitModel()
    {
        UserId = Guid.Empty.ToString();
    }
    public string UserId { get; set; }
    [AuditIgnore]
    public UserModel User { get; set; }
    public long? MaxFileSize { get; set; }
    public long? MaxStorage { get; set; }

    [DefaultValue(0)]
    public long SpaceUsed { get; set; }

    /// <summary>
    /// Space used for file previews.
    /// </summary>
    [DefaultValue(0)]
    public long PreviewSpaceUsed { get; set; }
}