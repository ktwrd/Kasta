using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kasta.Data.Models.Audit;

public class AuditEntryModel
{
    public AuditEntryModel()
    {
        Id = Guid.NewGuid().ToString();
    }
    public const string TableName = "AuditEntry";
    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }

    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    [ForeignKey(nameof(Audit))]
    public string AuditId { get; set; }
    [AuditIgnore]
    public AuditModel Audit { get; set; }

    [Required]
    [MaxLength(200)]
    public string PropertyName { get; set; }
    public string? Value { get; set; }
}