using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kate.FileShare.Data.Models.Audit;

public class AuditModel
{
    public AuditModel()
    {
        Id = Guid.NewGuid().ToString();
    }
    public const string TableName = "Audit";
    public string Id { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="UserModel"/>
    /// </summary>
    [Required]
    [ForeignKey(nameof(CreatedByUser))]
    public string CreatedBy { get; set; }
    public UserModel CreatedByUser { get; set; }
    [Required]
    public DateTimeOffset CreatedAt { get; set; }
    [Required]
    public string EntityName { get; set; }
    [Required]
    public string PrimaryKey { get; set; }

    public AuditEventKind Kind { get; set; }

    [InverseProperty(nameof(AuditEntryModel.Audit))]
    public List<AuditEntryModel> Entries { get; set; } = [];
}