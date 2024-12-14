using System.Collections.ObjectModel;
using Kasta.Web.Models;

namespace Kasta.Web.Areas.Admin.Models.User;

public class RoleDetailsComponentViewModel : BaseAlertViewModel
{
    public required string UserId { get; set; }

    public List<string> UserRoleIds { get; set; } = [];

    /// <summary>
    /// <para>Key: Role ID</para>
    /// <para>Value: Name</para>
    /// </summary>
    
    public Dictionary<string, string> Roles { get; set; } = [];
}