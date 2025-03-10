using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kasta.Data.Models.Audit;

public class AuditModel
{
    public AuditModel()
    {
        Id = Guid.NewGuid().ToString();
    }
    public const string TableName = "Audit";
    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
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
    [MaxLength(200)]
    public string EntityName { get; set; }
    [Required]
    [MaxLength(200)]
    public string PrimaryKey { get; set; }

    public AuditEventKind Kind { get; set; }

    [AuditIgnore]
    [InverseProperty(nameof(AuditEntryModel.Audit))]
    public List<AuditEntryModel> Entries { get; set; } = [];
}