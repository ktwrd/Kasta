using System.ComponentModel.DataAnnotations;

namespace Kasta.Data.Models;

public class TrustedProxyHeaderMappingModel
{
    public const string TableName = "Config_TrustedProxyHeaderMapping";
    public TrustedProxyHeaderMappingModel()
    {
        Id = Guid.NewGuid().ToString();
    }
    
    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="TrustedProxyHeaderModel.Id"/>
    /// </summary>
    [MaxLength(DatabaseHelper.GuidLength)]
    public string? TrustedProxyHeaderId { get; set; }

    [AuditIgnore]
    public TrustedProxyHeaderModel? TrustedProxyHeader { get; set; }
    
    /// <summary>
    /// Foreign Key to <see cref="TrustedProxyModel.Id"/>
    /// </summary>
    [MaxLength(DatabaseHelper.GuidLength)]
    public string? TrustedProxyId { get; set; }
    
    [AuditIgnore]
    public TrustedProxyModel? TrustedProxy { get; set; }
}