using System.ComponentModel;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Helpers;

namespace Kasta.Web.Models;

public class SystemSettingsParams
{
    [DefaultValue(true)]
    public bool EnableUserRegister { get; set; } = true;
    [DefaultValue(true)]
    public bool EnableEmbeds { get; set; } = true;
    [DefaultValue(false)]
    public bool EnableCustomBranding { get; set; } = false;
    [DefaultValue("Powered by Kasta")]
    public string CustomBrandingFooter { get; set; } = "Powered by Kasta";
    [DefaultValue("Kasta")]
    public string CustomBrandingTitle { get; set; } = "Kasta";
    [DefaultValue(false)]
    public bool EnableQuota { get; set; } = false;
    [DefaultValue("")]
    public string DefaultUploadQuota {get; set; } = "";
    [DefaultValue("")]
    public string DefaultStorageQuota { get; set; } = "";

    public long? DefaultUploadQuotaReal
    {
        get
        {
            return SizeHelper.ParseToByteCount(DefaultUploadQuota);
        }
    }

    public long? DefaultStorageQuotaReal
    {
        get
        {
            return SizeHelper.ParseToByteCount(DefaultStorageQuota);
        }
    }
    private PreferencesModel GetPreferenceModel(ApplicationDbContext db, string key)
    {
        var d = db.Preferences.FirstOrDefault(e => e.Key == key);
        if (d == null)
        {
            d = new()
            {
                Key = key
            };
            db.Preferences.Add(d);
        }
        return d;
    }
    public void InsertOrUpdate(ApplicationDbContext db)
    {
        var enableUserRegister = GetPreferenceModel(db, "enableUserRegister");
        enableUserRegister.Set(EnableUserRegister);

        var embedEnable = GetPreferenceModel(db, "embedEnable");
        embedEnable.Set(EnableEmbeds);

        var customBrandEnable = GetPreferenceModel(db, "customBrandEnable");
        customBrandEnable.Set(EnableCustomBranding);

        var customBrandFooter = GetPreferenceModel(db, "customBrandFooter");
        customBrandFooter.Set(CustomBrandingFooter);

        var customBrandTitle = GetPreferenceModel(db, "customBrandTitle");
        customBrandTitle.Set(CustomBrandingTitle);

        var quotaEnable = GetPreferenceModel(db, "quotaEnable");
        quotaEnable.Set(EnableQuota);

        var defaultUploadQuota = GetPreferenceModel(db, "defaultUploadQuota");
        defaultUploadQuota.Set(SizeHelper.ParseToByteCount(DefaultUploadQuota));

        var defaultStorageQuota = GetPreferenceModel(db, "defaultStorageQuota");
        defaultStorageQuota.Set(SizeHelper.ParseToByteCount(DefaultStorageQuota));
    }

    public void Get(ApplicationDbContext db)
    {
        var enableUserRegister = GetPreferenceModel(db, "enableUserRegister");
        EnableUserRegister = enableUserRegister.GetBool(true);

        var embedEnable = GetPreferenceModel(db, "embedEnable");
        EnableEmbeds = embedEnable.GetBool(true);

        var customBrandEnable = GetPreferenceModel(db, "customBrandEnable");
        EnableCustomBranding = customBrandEnable.GetBool(false);

        var customBrandFooter = GetPreferenceModel(db, "customBrandFooter");
        CustomBrandingFooter = customBrandFooter.GetString(null) ?? "";

        var customBrandTitle = GetPreferenceModel(db, "customBrandTitle");
        CustomBrandingTitle = string.IsNullOrEmpty(customBrandTitle.Value) ? "Kasta.Web" : customBrandTitle.GetString("Kasta.Web") ?? "Kasta.Web";

        var quotaEnable = GetPreferenceModel(db, "quotaEnable");
        EnableQuota = quotaEnable.GetBool(false);

        var defaultUploadQuota = GetPreferenceModel(db, "defaultUploadQuota");
        DefaultUploadQuota = defaultUploadQuota.GetLong(null)?.ToString() ?? "";

        var defaultStorageQuota = GetPreferenceModel(db, "defaultStorageQuota");
        DefaultStorageQuota = defaultStorageQuota.GetLong(null)?.ToString() ?? "";
    }
}