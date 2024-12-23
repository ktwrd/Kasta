using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kasta.Data.Models;

public class TrustedProxyHeaderModel
{
    public const string TableName = "Config_TrustedProxyHeader";

    public TrustedProxyHeaderModel()
    {
        Id = Guid.NewGuid().ToString();
        HeaderName = "";
        HeaderMappings = [];
    }

    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }
    
    /// <summary>
    /// Name of the request header to treat as the real remote IP Address.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string HeaderName { get; set; }

    [DefaultValue(false)]
    public bool Enable { get; set; }

    [AuditIgnore]
    public List<TrustedProxyHeaderMappingModel> HeaderMappings { get; set; }
}