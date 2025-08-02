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
        CreatedAt = DateTimeOffset.UtcNow;
        Token = GenerateToken();
        CreatedByUserId = null;
    }
    private static string GenerateToken()
    {
        int length = 20;
        const string valid = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        StringBuilder res = new StringBuilder();
        Random rnd = new Random();
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
    
    [AuditIgnore]
    public UserModel User { get; set; }
    
    [MaxLength(200)]
    public string? Purpose { get; set; }

    [MaxLength(100)]
    public string Token { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// <see cref="UserModel.Id"/> that created this Api Key.
    /// </summary>
    [MaxLength(DatabaseHelper.GuidLength)]
    public string? CreatedByUserId { get; set; }

    [AuditIgnore]
    public UserModel? CreatedByUser { get; set; }
}