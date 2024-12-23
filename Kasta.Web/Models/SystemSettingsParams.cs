using System.ComponentModel;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Web.Helpers;

namespace Kasta.Web.Models;

public class SystemSettingsParams
{
    public bool EnableUserRegister { get; set; }
    public bool EnableEmbeds { get; set; }
    public bool EnableLinkShortener { get; set; }
    public bool EnableCustomBranding { get; set; }
    public string CustomBrandingTitle { get; set; } = "Kasta";
    public bool EnableQuota { get; set; }
    public string DefaultUploadQuota {get; set; } = "";
    public string DefaultStorageQuota { get; set; } = "";
    public bool EnableGeoIP { get; set; }
    public string GeoIPDatabaseLocation { get;set; } = "";

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
    private PreferencesModel GetPreferenceModel(ApplicationDbContext db, string key, bool insert = true)
    {
        var d = db.Preferences.FirstOrDefault(e => e.Key == key);
        if (d == null)
        {
            d = new()
            {
                Key = key
            };
            if (insert)
            {
                db.Preferences.Add(d);
            }
        }
        return d;
    }
    public void InsertOrUpdate(ApplicationDbContext db)
    {
        var enableUserRegister = GetPreferenceModel(db, "enableUserRegister", insert: false);
        enableUserRegister.Set(EnableUserRegister);

        var embedEnable = GetPreferenceModel(db, "embedEnable", insert: false);
        embedEnable.Set(EnableEmbeds);

        var enableLinkShortener = GetPreferenceModel(db, "enableLinkShortener", insert: false);
        enableLinkShortener.Set(EnableLinkShortener);

        var customBrandEnable = GetPreferenceModel(db, "customBrandEnable", insert: false);
        customBrandEnable.Set(EnableCustomBranding);

        var customBrandTitle = GetPreferenceModel(db, "customBrandTitle", insert: false);
        customBrandTitle.Set(CustomBrandingTitle);

        var quotaEnable = GetPreferenceModel(db, "quotaEnable", insert: false);
        quotaEnable.Set(EnableQuota);

        var defaultUploadQuota = GetPreferenceModel(db, "defaultUploadQuota", insert: false);
        defaultUploadQuota.Set(SizeHelper.ParseToByteCount(DefaultUploadQuota));

        var defaultStorageQuota = GetPreferenceModel(db, "defaultStorageQuota", insert: false);
        defaultStorageQuota.Set(SizeHelper.ParseToByteCount(DefaultStorageQuota));

        var enableGeoIP = GetPreferenceModel(db, "enableGeoIP", insert: false);
        enableGeoIP.Set(EnableGeoIP);

        var geoIPDbLocation = GetPreferenceModel(db, "geoIPDbLocation", insert: false);
        geoIPDbLocation.Set(GeoIPDatabaseLocation);
    }

    public void Get(ApplicationDbContext db)
    {
        var enableUserRegister = GetPreferenceModel(db, "enableUserRegister");
        EnableUserRegister = enableUserRegister.GetBool(true);

        var embedEnable = GetPreferenceModel(db, "embedEnable");
        EnableEmbeds = embedEnable.GetBool(true);

        var enableLinkShortener = GetPreferenceModel(db, "enableLinkShortener");
        EnableLinkShortener = enableLinkShortener.GetBool(false);

        var customBrandEnable = GetPreferenceModel(db, "customBrandEnable");
        EnableCustomBranding = customBrandEnable.GetBool(false);

        var customBrandTitle = GetPreferenceModel(db, "customBrandTitle");
        CustomBrandingTitle = string.IsNullOrEmpty(customBrandTitle.Value) ? "Kasta.Web" : customBrandTitle.GetString("Kasta.Web") ?? "Kasta.Web";

        var quotaEnable = GetPreferenceModel(db, "quotaEnable");
        EnableQuota = quotaEnable.GetBool(false);

        var defaultUploadQuota = GetPreferenceModel(db, "defaultUploadQuota");
        DefaultUploadQuota = defaultUploadQuota.GetLong(null)?.ToString() ?? "";

        var defaultStorageQuota = GetPreferenceModel(db, "defaultStorageQuota");
        DefaultStorageQuota = defaultStorageQuota.GetLong(null)?.ToString() ?? "";

        var enableGeoIP = GetPreferenceModel(db, "enableGeoIP");
        EnableGeoIP = enableGeoIP.GetBool(false);

        var geoIPDbLocation = GetPreferenceModel(db, "geoIPDbLocation");
        GeoIPDatabaseLocation = geoIPDbLocation.GetString("") ?? "";
    }
}