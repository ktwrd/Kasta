using Kasta.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Kasta.Web.Areas.Admin.Models.User;

public class UserDetailsViewModel
{
    public UserModel User { get; set; }
    public Dictionary<string, IdentityRole> Roles { get; set; } = [];
    public Dictionary<string, string> UserRoles { get; set; } = [];
}