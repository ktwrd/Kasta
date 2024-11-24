using Microsoft.AspNetCore.Identity;

namespace kate.FileShare.Data.Models;

public class UserLimitModel
{
    public string UserId { get; set; }
    public UserModel User { get; set; }
    public int? MaxFileSize { get; set; }
    public int? MaxStorage { get; set; }
}