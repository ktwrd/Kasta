using System.Collections.ObjectModel;

namespace Kasta.Web.Areas.Admin.Models.User;

public class RoleDetailsComponentViewModel
{
    public required string UserId { get; set; }

    public List<string> UserRoleIds { get; set; } = [];

    /// <summary>
    /// <para>Key: Role ID</para>
    /// <para>Value: Name</para>
    /// </summary>
    
    public Dictionary<string, string> Roles { get; set; } = [];
    
    public string? AlertType { get; set; }
    public string? AlertClass
    {
        get
        {
            if (string.IsNullOrEmpty(AlertType))
                return null;
            
            var t = AlertType.Trim().ToLower();
            if (ValidAlertTypes.Contains(t))
                return $"alert alert-{t}";
            return "alert alert-secondary";
        }
    }
    private static ReadOnlyCollection<string> ValidAlertTypes => new List<string>()
    {
        "primary",
        "secondary",
        "success",
        "danger",
        "warning",
        "info"
    }.AsReadOnly();
    public string? AlertContent { get; set; }
}