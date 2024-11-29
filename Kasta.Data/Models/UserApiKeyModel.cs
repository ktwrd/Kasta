using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kasta.Data.Models;

public class UserApiKeyModel
{
    public const string TableName = "UserApiKeys";
    public UserApiKeyModel()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTimeOffset.UtcNow;
        Token = GenerateToken();
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
    public string Id { get; set; }
    public string UserId { get; set; }
    public UserModel User { get; set; }
    [MaxLength(200)]
    public string? Purpose { get; set; }

    [MaxLength(100)]
    public string Token { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }
    public UserModel? CreatedByUser { get; set; }
}