using Kasta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web;

public class SystemSettingsProxy
{
    private readonly ApplicationDbContext _db;
    public SystemSettingsProxy(ApplicationDbContext db)
    {
        _db = db;
    }

    private void SetValue(string key, bool value)
    {
        using (var ctx = _db.CreateSession())
        {
            var trans = ctx.Database.BeginTransaction();
            try
            {
                if (ctx.Preferences.Any(e => e.Key == key))
                {
                    ctx.Preferences.Where(e => e.Key == key)
                        .ExecuteUpdate(e => e.SetProperty(v => v.Value, value ? "1" : "0"));
                }
                else
                {
                    ctx.Preferences.Add(new Data.Models.PreferencesModel()
                    {
                        Value = value ? "1" : "0",
                        Key = key,
                        ValueKind = "int"
                    });
                }
                ctx.SaveChanges();
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
    }

    private void SetValue(string key, string value)
    {
        using (var ctx = _db.CreateSession())
        {
            var trans = ctx.Database.BeginTransaction();
            try
            {
                if (ctx.Preferences.Any(e => e.Key == key))
                {
                    ctx.Preferences.Where(e => e.Key == key && e.ValueKind == "string")
                        .ExecuteUpdate(e => e.SetProperty(v => v.Value, value));
                }
                else
                {
                    ctx.Preferences.Add(new Data.Models.PreferencesModel()
                    {
                        Value = value,
                        Key = key,
                        ValueKind = "string"
                    });
                }
                ctx.SaveChanges();
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
    }
    private void SetValue(string key, long? value)
    {
        using (var ctx = _db.CreateSession())
        {
            var dbValue = value?.ToString() ?? "";
            var trans = ctx.Database.BeginTransaction();
            try
            {
                if (ctx.Preferences.Any(e => e.Key == key))
                {
                    ctx.Preferences.Where(e => e.Key == key && e.ValueKind == "long")
                        .ExecuteUpdate(e => e.SetProperty(v => v.Value, dbValue));
                }
                else
                {
                    ctx.Preferences.Add(new Data.Models.PreferencesModel()
                    {
                        Value = dbValue,
                        Key = key,
                        ValueKind = "long"
                    });
                }
                ctx.SaveChanges();
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
    }
    private bool GetBool(string key, bool defaultValue)
    {
        var result = _db.Preferences
            .Select(e => new { e.Value, e.Key })
            .FirstOrDefault(e => e.Key == key);
        if (string.IsNullOrEmpty(result?.Value))
        {
            return defaultValue;
        }
        else
        {
            return result.Value == "1";
        }
    }
    private string GetString(string key, string defaultValue)
    {
        var result = _db.Preferences
            .Select(e => new { e.Value, e.Key })
            .FirstOrDefault(e => e.Key == key);
        if (string.IsNullOrEmpty(result?.Value))
        {
            return defaultValue;
        }
        else
        {
            return result.Value;
        }
    }
    private long? GetLong(string key, long? defaultValue)
    {
        var result = _db.Preferences
            .Select(e => new { e.Value, e.Key, e.ValueKind })
            .FirstOrDefault(e => e.Key == key && e.ValueKind == "long");
        if (result == null)
        {
            return defaultValue;
        }
        else
        {
            if (long.TryParse(result.Value, out var v))
                return v;
            return defaultValue;
        }
    }

    private const string EnableUserRegisterKey = "enableUserRegister";
    public const bool EnableUserRegisterDefault = true;
    public bool EnableUserRegister
    {
        get => GetBool(EnableUserRegisterKey, EnableUserRegisterDefault);
        set => SetValue(EnableUserRegisterKey, value);
    }
    public const string EnableEmbedsKey = "embedEnable";
    public const bool EnableEmbedsDefault = true;
    public bool EnableEmbeds
    {
        get => GetBool(EnableEmbedsKey, EnableEmbedsDefault);
        set => SetValue(EnableEmbedsKey, value);
    }
    public const string EnableLinkShortenerKey = "enableLinkShortener";
    public const bool EnableLinkShortenerDefault = false;
    public bool EnableLinkShortener
    {
        get => GetBool(EnableLinkShortenerKey, EnableLinkShortenerDefault);
        set => SetValue(EnableLinkShortenerKey, value);
    }

    public const string EnableCustomBrandingKey = "customBrandEnable";
    public const bool EnableCustomBrandingDefault = false;
    public bool EnableCustomBranding
    {
        get => GetBool(EnableCustomBrandingKey, EnableCustomBrandingDefault);
        set => SetValue(EnableCustomBrandingKey, value);
    }

    public const string CustomBrandingTitleKey = "customBrandTitle";
    public const string CustomBrandingTitleDefault = "Kasta";
    public string CustomBrandingTitle
    {
        get
        {
            var result = GetString(CustomBrandingTitleKey, CustomBrandingTitleDefault);
            return string.IsNullOrEmpty(result) ? CustomBrandingTitleDefault : result;
        }
        set => SetValue(CustomBrandingTitleKey, value);
    }

    public const string EnableQuotaKey = "quotaEnable";
    public const bool EnableQuotaDefault = false;
    public bool EnableQuota
    {
        get => GetBool(EnableQuotaKey, EnableQuotaDefault);
        set => SetValue(EnableQuotaKey, value);
    }
    public const string DefaultUploadQuotaKey = "defaultUploadQuota";
    public long? DefaultUploadQuota
    {
        get => GetLong(DefaultUploadQuotaKey, null);
        set => SetValue(DefaultUploadQuotaKey, value);
    }
    public const string DefaultStorageQuotaKey = "defaultStorageQuota";
    public long? DefaultStorageQuota
    {
        get => GetLong(DefaultStorageQuotaKey, null);
        set => SetValue(DefaultStorageQuotaKey, value);
    }
    public const string EnableGeoIpKey = "enableGeoIP";
    public const bool EnableGeoIpDefault = false;
    public bool EnableGeoIp
    {
        get => GetBool(EnableGeoIpKey, EnableGeoIpDefault);
        set => SetValue(EnableGeoIpKey, value);
    }
    public const string GeoIpDatabaseLocationKey = "geoIPDbLocation";
    public const string GeoIpDatabaseLocationDefault = "";
    public string GeoIpDatabaseLocation
    {
        get
        {
            var result = GetString(GeoIpDatabaseLocationKey, GeoIpDatabaseLocationDefault);
            return string.IsNullOrEmpty(result)
                ? GeoIpDatabaseLocationDefault
                : result;
        }
        set => SetValue(GeoIpDatabaseLocationKey, value);
    }

    public const string S3UsePresignedUrlKey = "s3_usePresignedUrl";
    public const bool S3UsePresignedUrlDefault = false;
    public bool S3UsePresignedUrl
    {
        get => GetBool(S3UsePresignedUrlKey, S3UsePresignedUrlDefault);
        set => SetValue(S3UsePresignedUrlKey, value);
    }
}