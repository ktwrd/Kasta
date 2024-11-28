using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kasta.Web.Data.Models.Audit;

public class AuditEntryModel
{
    public AuditEntryModel()
    {
        Id = Guid.NewGuid().ToString();
    }
    public const string TableName = "AuditEntry";
    public string Id { get; set; }

    [Required]
    [ForeignKey(nameof(Audit))]
    public string AuditId { get; set; }
    public AuditModel Audit { get; set; }

    [Required]
    public string PropertyName { get; set; }
    public string? Value { get; set; }
}