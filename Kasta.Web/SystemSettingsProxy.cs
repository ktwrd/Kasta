using EasyCaching.Core;
using Kasta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kasta.Web;

/// <summary>
/// Proxy class for easily accessing global settings defined in <see cref="ApplicationDbContext.Preferences"/>
/// </summary>
public class SystemSettingsProxy
{
    private static string GetCacheKey(string key)
    {
        return $"{nameof(SystemSettingsProxy)} {key}";
    }
    internal record SystemSettingsProxyCacheValue(string Key, string? Value, string ValueKind)
    {
        public string GetKey() => GetCacheKey(Key);
    };
    private readonly IEasyCachingProvider _caching;
    private readonly ApplicationDbContext _db;
    public SystemSettingsProxy(ApplicationDbContext db, IEasyCachingProvider cachingProvider)
    {
        _db = db;
        _caching = cachingProvider;
    }

    private const string ValueKindString = "string";
    private const string ValueKindBool = "int";
    private const string ValueKindInt = "int";
    private const string ValueKindLong = "long";

    private void UpdateOrInsert(string key, string value, string valueKind, bool enforceValueKind = false)
    {
        using var ctx = _db.CreateSession();
        var trans = ctx.Database.BeginTransaction();
        try
        {
            if (ctx.Preferences.Any(e => e.Key == key))
            {
                if (enforceValueKind)
                {
                    ctx.Preferences.Where(e => e.Key == key && e.ValueKind == valueKind)
                        .ExecuteUpdate(e => e.SetProperty(v => v.Value, value));
                }
                else
                {
                    ctx.Preferences.Where(e => e.Key == key)
                        .ExecuteUpdate(e => e.SetProperty(v => v.Value, value));
                }
            }
            else
            {
                ctx.Preferences.Add(new Data.Models.PreferencesModel()
                {
                    Value = value,
                    Key = key,
                    ValueKind = valueKind
                });
            }
            ctx.SaveChanges();
            trans.Commit();
            var cacheValue = new SystemSettingsProxyCacheValue(key, value, valueKind);
            _caching.Set(cacheValue.GetKey(), cacheValue, TimeSpan.FromSeconds(30));
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    private string? ReadValue(string key, string valueKind, bool enforceValueKind = false)
    {
        var cacheValue = _caching.Get<SystemSettingsProxyCacheValue>(GetCacheKey(key));
        if (cacheValue.HasValue) return cacheValue.Value.Value;
        return ReadDatabaseValue(key, valueKind, enforceValueKind);
    }
    private string? ReadDatabaseValue(string key, string valueKind, bool enforceValueKind = false)
    {
        if (enforceValueKind)
        {
            return _db.Preferences.Where(e => e.Key == key && e.ValueKind == valueKind)
                .Select(e => e.Value)
                .FirstOrDefault();
        }
        
        return _db.Preferences.Where(e => e.Key == key)
            .Select(e => e.Value)
            .FirstOrDefault();
    }

    private void SetValue(string key, bool value)
    {
        UpdateOrInsert(key, value ? "1" : "0", ValueKindBool);
    }

    private void SetValue(string key, string value)
    {
        UpdateOrInsert(key, value, ValueKindString);
    }
    private void SetValue(string key, long? value)
    {
        UpdateOrInsert(key, value?.ToString() ?? "", ValueKindLong);
    }
    private bool GetBool(string key, bool defaultValue)
    {
        var value = ReadValue(key, ValueKindBool, enforceValueKind: true);
        if (string.IsNullOrEmpty(value?.Trim()))
        {
            return defaultValue;
        }
        else
        {
            return value == "1";
        }
    }
    private string GetString(string key, string defaultValue)
    {
        var value = ReadValue(key, ValueKindString, enforceValueKind: false);
        if (string.IsNullOrEmpty(value?.Trim()))
        {
            return defaultValue;
        }
        else
        {
            return value;
        }
    }
    private long? GetLong(string key, long? defaultValue)
    {
        var value = ReadValue(key, ValueKindLong, enforceValueKind: true);
        if (value == null)
        {
            return defaultValue;
        }
        else
        {
            if (long.TryParse(value, out var v))
                return v;
            return defaultValue;
        }
    }
    private int? GetInt(string key, int? defaultValue)
    {
        var value = ReadValue(key, ValueKindInt, enforceValueKind: true);
        if (value == null)
        {
            return defaultValue;
        }
        else
        {
            if (int.TryParse(value, out var v))
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

    public const string FileServiceGenerateFileMetadataThreadCountKey = "fileService_generateFileMetadata_threadCount";
    public const int FileServiceGenerateFileMetadataThreadCountDefault = 0;
    public int FileServiceGenerateFileMetadataThreadCount
    {
        get => Math.Min(
            GetInt(FileServiceGenerateFileMetadataThreadCountKey, FileServiceGenerateFileMetadataThreadCountDefault)
                ?? FileServiceGenerateFileMetadataThreadCountDefault, 
            FileServiceGenerateFileMetadataThreadCountDefault);
        set => SetValue(FileServiceGenerateFileMetadataThreadCountKey, value);
    }

    public const string FileServicePlainTextPreviewSizeLimitKey = "fileService_plainTextPreview_maxSize";
    public const long FileServicePlainTextPreviewSizeLimitDefault = 524_288;
    public long? FileServicePlainTextPreviewSizeLimit
    {
        get
        {
            var value = GetLong(FileServicePlainTextPreviewSizeLimitKey, FileServicePlainTextPreviewSizeLimitDefault);
            if (value.HasValue) return Math.Max(0, value.Value);
            return value;
        }
        set
        {
            SetValue(FileServicePlainTextPreviewSizeLimitKey, value.HasValue ? Math.Max(0, value.Value) : null);
        }
    }
}