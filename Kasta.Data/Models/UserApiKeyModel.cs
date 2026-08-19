using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kasta.Data.Models;

public class UserApiKeyModel
{
    public const string TableName = "UserApiKeys";
    public UserApiKeyModel()
    {
        Id = Guid.NewGuid().ToString();
        UserId = Guid.Empty.ToString();
        CreatedAt = DateTime.UtcNow;
        Token = GenerateToken();
        CreatedByUserId = null;
        User = null!;
    }
    private static string GenerateToken()
    {
        int length = 20;
        const string valid = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        var res = new StringBuilder();
        var rnd = new Random();
        while (0 < length--)
        {
            res.Append(valid[rnd.Next(valid.Length)]);
        }
        return res.ToString();
    }
    
    /// <summary>
    /// Primary Key & Unique Identifier for this Api Key
    /// </summary>
    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }

    /// <summary>
    /// <see cref="UserModel.Id"/> that this Api Key is for
    /// </summary>
    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string UserId { get; set; }
    
    /// <summary>
    /// Api Key purpose
    /// </summary>
    [MaxLength(200)]
    public string? Purpose { get; set; }

    /// <summary>
    /// Api Key token
    /// </summary>
    [MaxLength(100)]
    [MinLength(18)]
    public string Token { get; set; }
    
    /// <summary>
    /// When this Api Key was last used (UTC)
    /// </summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// <see cref="UserModel.Id"/> that created this Api Key.
    /// </summary>
    [MaxLength(DatabaseHelper.GuidLength)]
    public string? CreatedByUserId { get; set; }
    
    /// <summary>
    /// When this API Key was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// User Agent that created this Api Key
    /// </summary>
    [MaxLength(500)]
    public string? CreatedByUserAgent { get; set; }
    
    /// <summary>
    /// IP Address that created this Api Key
    /// </summary>
    [MaxLength(100)]
    public string? CreatedByIpAddress { get; set; }
    
    [DefaultValue(false)]
    public bool IsDeleted { get; set; }

    [MaxLength(DatabaseHelper.GuidLength)]
    public string? DeletedByUserId { get; set; }
    /// <summary>
    /// When this Api Key was deleted (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    // TODO refactor this to be user session info stored in the database (e.g: ips used, user agents used, tz, approx location, etc...)
    [MaxLength(500)]
    public string? DeletedByUserAgent { get; set; }
    [MaxLength(100)]
    public string? DeletedByIpAddress { get; set; }

    #region Property Accessors
    [AuditIgnore]
    public UserModel User { get; set; }
    [AuditIgnore]
    public UserModel? CreatedByUser { get; set; }
    [AuditIgnore]
    public UserModel? DeletedByUser { get; set; }
    #endregion
}