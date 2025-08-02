using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kasta.Data.Models.Audit;

public class AuditModel
{
    public const string TableName = "Audit";
    public AuditModel()
    {
        Id = Guid.NewGuid().ToString();
    }

    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="UserModel"/>
    /// </summary>
    [Required]
    [ForeignKey(nameof(CreatedByUser))]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string CreatedBy { get; set; }

    [AuditIgnore]
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