using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kasta.Data.Models;

public class UserSettingModel
{
    public const string TableName = "UserSettings";

    public UserSettingModel()
    {
        Id = Guid.Empty.ToString();
    }

    /// <summary>
    /// Foreign Key to <see cref="UserModel"/>
    /// </summary>
    [Required]
    public string Id { get; set; }
    [AuditIgnore]
    public UserModel User { get; set; }

    [MaxLength(100)]
    public string? ThemeName { get; set; }

    [DefaultValue(true)]
    public bool ShowFilePreviewInHome { get; set; } = true;
}