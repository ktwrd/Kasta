using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kasta.Data.Models;

public class ShortLinkModel
{
    public const string TableName = "ShortLinks";

    public ShortLinkModel()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTimeOffset.UtcNow;
        Destination = "";
        IsVanity = false;
    }

    public string Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }
    [AuditIgnore]
    public UserModel? CreatedByUser { get; set; }

    [MaxLength(1000)]
    public string Destination { get; set; }

    [MaxLength(100)]
    public string? ShortLink { get; set; }

    [DefaultValue(false)]
    public bool IsVanity { get; set; }
}