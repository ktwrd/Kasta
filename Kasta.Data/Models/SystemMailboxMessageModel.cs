using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kasta.Data.Models;

public class SystemMailboxMessageModel
{
    public const string TableName = "SystemMailboxMessage";

    public SystemMailboxMessageModel()
    {
        Id = Guid.NewGuid().ToString();
        Subject = "System Message";
        Message = "";
        CreatedAt = DateTimeOffset.UtcNow;
        Seen = false;
        IsDeleted = false;
    }

    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }
    
    [Required]
    [MaxLength(SubjectMaxLength)]
    public string Subject { get; set; }

    public const int SubjectMaxLength = 300;
    
    [Required]
    [MaxLength(MessageMaxLength)]
    public string Message { get; set; }

    public const int MessageMaxLength = 8192;
    
    [Required]
    public DateTimeOffset CreatedAt { get; set; }
    
    [DefaultValue(false)]
    public bool Seen { get; set; }
    
    [DefaultValue(false)]
    public bool IsDeleted { get; set; }
}