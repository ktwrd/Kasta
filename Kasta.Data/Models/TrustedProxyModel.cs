using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kasta.Data.Models;

public class TrustedProxyModel
{
    public const string TableName = "Config_TrustedProxy";

    public TrustedProxyModel()
    {
        Id = Guid.NewGuid().ToString();
        Address = "";
        HeaderMappings = [];
    }

    [Required]
    [MaxLength(DatabaseHelper.GuidLength)]
    public string Id { get; set; }

    /// <summary>
    /// Only trust this proxy when the "Host" header is equal to this value.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Address { get; set; }

    [DefaultValue(false)]
    public bool Enable { get; set; }
    
    [AuditIgnore]
    public List<TrustedProxyHeaderMappingModel> HeaderMappings { get; set; }
}