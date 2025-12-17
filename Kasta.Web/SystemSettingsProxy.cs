using System.Reflection;
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

    public void EnsureInitialized()
    {
        var existingNames = _db.Preferences.Select(e => e.Key).ToList();
        using var ctx = _db.CreateSession();
        using var trans = ctx.Database.BeginTransaction();
        foreach (var prop in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var defaultValueProp = typeof(DefaultValues).GetField(prop.Name);
            var keyProp = typeof(Keys).GetField(prop.Name) ??
                          throw new InvalidOperationException($"Could not find Key for property {prop.Name}");
            if (existingNames.Contains((string)keyProp.GetValue(null)!))
            {
                continue;
            }

            if (defaultValueProp == null)
            {
                continue;
            }

            prop.SetValue(this, defaultValueProp.GetValue(null));
        }
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

    public bool EnableUserRegister
    {
        get => GetBool(Keys.EnableUserRegister, DefaultValues.EnableUserRegister);
        set => SetValue(Keys.EnableUserRegister, value);
    }
    public bool EnableEmbeds
    {
        get => GetBool(Keys.EnableEmbeds, DefaultValues.EnableEmbeds);
        set => SetValue(Keys.EnableEmbeds, value);
    }
    public bool EnableLinkShortener
    {
        get => GetBool(Keys.EnableLinkShortener, DefaultValues.EnableLinkShortener);
        set => SetValue(Keys.EnableLinkShortener, value);
    }

    public bool EnableCustomBranding
    {
        get => GetBool(Keys.EnableCustomBranding, DefaultValues.EnableCustomBranding);
        set => SetValue(Keys.EnableCustomBranding, value);
    }

    public string CustomBrandingTitle
    {
        get
        {
            var result = GetString(Keys.CustomBrandingTitle, DefaultValues.CustomBrandingTitle);
            return string.IsNullOrEmpty(result.Trim()) ? DefaultValues.CustomBrandingTitle : result;
        }
        set => SetValue(Keys.CustomBrandingTitle, value);
    }

    public bool EnableQuota
    {
        get => GetBool(Keys.EnableQuota, DefaultValues.EnableQuota);
        set => SetValue(Keys.EnableQuota, value);
    }
    public long? DefaultUploadQuota
    {
        get => GetLong(Keys.DefaultUploadQuota, null);
        set => SetValue(Keys.DefaultUploadQuota, value is >= 0 ? value : null);
    }

    public long? DefaultStorageQuota
    {
        get => GetLong(Keys.DefaultStorageQuota, null);
        set => SetValue(Keys.DefaultStorageQuota, value is >= 0 ? value : null);
    }

    public bool EnableGeoIp
    {
        get => GetBool(Keys.EnableGeoIp, DefaultValues.EnableGeoIp);
        set => SetValue(Keys.EnableGeoIp, value);
    }
    public string GeoIpDatabaseLocation
    {
        get
        {
            var result = GetString(Keys.GeoIpDatabaseLocation, DefaultValues.GeoIpDatabaseLocation);
            return string.IsNullOrEmpty(result)
                ? DefaultValues.GeoIpDatabaseLocation
                : result;
        }
        set => SetValue(Keys.GeoIpDatabaseLocation, value);
    }

    public bool S3UsePresignedUrl
    {
        get => GetBool(Keys.S3UsePresignedUrl, DefaultValues.S3UsePresignedUrl);
        set => SetValue(Keys.S3UsePresignedUrl, value);
    }

    public int FileServiceGenerateFileMetadataThreadCount
    {
        get => Math.Min(
            GetInt(Keys.FileServiceGenerateFileMetadataThreadCount, DefaultValues.FileServiceGenerateFileMetadataThreadCount)
                ?? DefaultValues.FileServiceGenerateFileMetadataThreadCount,
                DefaultValues.FileServiceGenerateFileMetadataThreadCount);
        set => SetValue(
            Keys.FileServiceGenerateFileMetadataThreadCount,
            Math.Max(value, DefaultValues.FileServiceGenerateFileMetadataThreadCount));
    }

    /// <summary>
    /// <para>File size limit in bytes.</para>
    /// <para>Default value: 512kb (524288)</para>
    /// </summary>
    /// <remarks>
    /// <list type="bullet" >
    /// <item>If this is <c>0</c>, then plaintext file previews are disabled.</item>
    /// <item>Otherwise, this is the maximum file size in bytes for a preview to be shown in plaintext.</item>
    /// </list>
    /// </remarks>
    public long? FileServicePlainTextPreviewSizeLimit
    {
        get
        {
            var value = GetLong(Keys.FileServicePlainTextPreviewSizeLimit, DefaultValues.FileServicePlainTextPreviewSizeLimit);
            return value.HasValue
                ? Math.Max(0, value.Value)
                : null;
        }
        set => SetValue(
            Keys.FileServicePlainTextPreviewSizeLimit,
            value.HasValue ? Math.Max(0, value.Value) : null);
    }

    public bool FileServicePlainTextPreviewSizeLimitEnforce
    {
        get => GetBool(Keys.FileServicePlainTextPreviewSizeLimitEnforce, DefaultValues.FileServicePlainTextPreviewSizeLimitEnforce);
        set => SetValue(Keys.FileServicePlainTextPreviewSizeLimitEnforce, value);
    }

    public bool FileServiceAllowPlainTextPreview
    {
        get => GetBool(Keys.FileServiceAllowPlainTextPreview, DefaultValues.FileServiceAllowPlainTextPreview);
        set => SetValue(Keys.FileServiceAllowPlainTextPreview, value);
    }

    public static class Keys
    {
        public const string EnableUserRegister = "enableUserRegister";
        public const string EnableEmbeds = "embedEnable";
        public const string EnableLinkShortener = "enableLinkShortener";
        public const string EnableCustomBranding = "customBrandEnable";
        public const string CustomBrandingTitle = "customBrandTitle";
        public const string EnableQuota = "quotaEnable";
        public const string DefaultUploadQuota = "defaultUploadQuota";
        public const string DefaultStorageQuota = "defaultStorageQuota";
        public const string EnableGeoIp = "enableGeoIP";
        public const string GeoIpDatabaseLocation = "geoIPDbLocation";
        public const string S3UsePresignedUrl = "s3_usePresignedUrl";
        public const string FileServiceGenerateFileMetadataThreadCount = "fileService_generateFileMetadata_threadCount";
        public const string FileServicePlainTextPreviewSizeLimit = "fileService_plainTextPreview_maxSize";
        public const string FileServicePlainTextPreviewSizeLimitEnforce = "fileService_plainTextPreview_maxSize_enforce";
        public const string FileServiceAllowPlainTextPreview = "fileService_plainTextPreview_enable";
    }

    public static class DefaultValues
    {
        public const bool EnableUserRegister = true;
        public const bool EnableEmbeds = true;
        public const bool EnableLinkShortener = false;
        public const bool EnableCustomBranding = false;
        public const string CustomBrandingTitle = "Kasta";
        public const bool EnableQuota = false;
        public const bool EnableGeoIp = false;
        public const string GeoIpDatabaseLocation = "";
        public const bool S3UsePresignedUrl = false;
        public const int FileServiceGenerateFileMetadataThreadCount = 0;
        public const long FileServicePlainTextPreviewSizeLimit = 524_288; // 512kb
        public const bool FileServicePlainTextPreviewSizeLimitEnforce = true;
        public const bool FileServiceAllowPlainTextPreview = true;
    }
}