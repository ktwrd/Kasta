using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kasta.Data.Models;

public class ShortLinkModel
{
    public const string TableName = "ShortLinks";

    public ShortLinkModel()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        Destination = "";
        IsVanity = false;
        CreatedByUser = null;
    }

    /// <summary>
    /// Primary Key (Guid as string)
    /// </summary>
    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }

    /// <summary>
    /// Time when this record was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User Id that created this record (FK to <see cref="UserModel.Id"/>
    /// </summary>
    [MaxLength(DatabaseHelper.GuidLength)]
    public string? CreatedByUserId { get; set; }

    /// <summary>
    /// Destination URL
    /// </summary>
    [MaxLength(1000)]
    public string Destination { get; set; }

    /// <summary>
    /// Shortened link. Can be auto-generated, or user provided (if they have the right permissions)
    /// </summary>
    [MaxLength(100)]
    public string? ShortLink { get; set; }

    /// <summary>
    /// Is <see cref="ShortLink"/> a value that was provided by a user?
    /// If so, then this is <see langword="true"/>
    /// </summary>
    [DefaultValue(false)]
    public bool IsVanity { get; set; }

    /// <summary>
    /// Property Accessor.
    /// </summary>
    [AuditIgnore]
    public UserModel? CreatedByUser { get; set; }
}