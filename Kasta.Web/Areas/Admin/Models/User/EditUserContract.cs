namespace Kasta.Web.Areas.Admin.Models.User;

public class EditUserContract
{
    public bool EnableStorageQuota { get; set; }
    public string? StorageQuotaValue { get; set; }
    public bool EnableUploadLimit { get; set; }
    public string? UploadLimitValue { get; set; }
}