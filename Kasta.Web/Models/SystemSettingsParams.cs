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
    public bool S3UsePresignedUrl { get; set; }
    public string S3PresignedUrlBase { get; set; } = "";

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
        var proxy = new SystemSettingsProxy(db);

        proxy.EnableUserRegister = EnableUserRegister;
        proxy.EnableEmbeds = EnableEmbeds;
        proxy.EnableLinkShortener = EnableLinkShortener;
        proxy.EnableCustomBranding = EnableCustomBranding;
        proxy.CustomBrandingTitle = CustomBrandingTitle;
        proxy.EnableQuota = EnableQuota;
        proxy.DefaultUploadQuota = SizeHelper.ParseToByteCount(DefaultUploadQuota);
        proxy.DefaultStorageQuota = SizeHelper.ParseToByteCount(DefaultStorageQuota);
        proxy.EnableGeoIp = EnableGeoIP;
        proxy.GeoIpDatabaseLocation = GeoIPDatabaseLocation;
        proxy.S3UsePresignedUrl = S3UsePresignedUrl;
        proxy.S3PresignedUrlBase = S3PresignedUrlBase;
    }

    public void Get(ApplicationDbContext db)
    {
        var proxy = new SystemSettingsProxy(db);

        EnableUserRegister = proxy.EnableUserRegister;
        EnableEmbeds = proxy.EnableEmbeds;
        EnableLinkShortener = proxy.EnableLinkShortener;
        EnableCustomBranding = proxy.EnableCustomBranding;
        CustomBrandingTitle = proxy.CustomBrandingTitle;
        EnableQuota = proxy.EnableQuota;
        DefaultUploadQuota = proxy.DefaultUploadQuota?.ToString() ?? "";
        DefaultStorageQuota = proxy.DefaultStorageQuota?.ToString() ?? "";
        EnableGeoIP = proxy.EnableGeoIp;
        GeoIPDatabaseLocation = proxy.GeoIpDatabaseLocation;
        S3UsePresignedUrl = proxy.S3UsePresignedUrl;
        S3PresignedUrlBase = proxy.S3PresignedUrlBase;
    }
}