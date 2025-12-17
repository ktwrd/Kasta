using Kasta.Shared.Helpers;

namespace Kasta.Web.Models;

public class SystemSettingsViewModel
{
    public bool EnableUserRegister { get; set; }
    public bool EnableEmbeds { get; set; }
    public bool EnableLinkShortener { get; set; }
    public bool EnableCustomBranding { get; set; }
    public string CustomBrandingTitle { get; set; } = "Kasta";
    public bool EnableQuota { get; set; }
    public string DefaultUploadQuota {get; set; } = "";
    public string DefaultStorageQuota { get; set; } = "";
    public string FileServiceGenerateFileMetadataThreadCount { get; set; } = "0";
    public string FileServicePlainTextPreviewSizeLimit { get; set; } = "0";
    public bool FileServicePlainTextPreviewSizeLimitEnforce { get; set; }
    public bool FileServiceAllowPlainTextPreview { get; set; }
    public bool EnableGeoIP { get; set; }
    public string GeoIPDatabaseLocation { get;set; } = "";
    public bool S3UsePresignedUrl { get; set; }

    public long? DefaultUploadQuotaReal => SizeHelper.ParseToByteCount(DefaultUploadQuota);
    public long? DefaultStorageQuotaReal => SizeHelper.ParseToByteCount(DefaultStorageQuota);
    public long? FileServicePlainTextPreviewSizeLimitReal => SizeHelper.ParseToByteCount(FileServicePlainTextPreviewSizeLimit);
    public int FileServiceGenerateFileMetadataThreadCountReal
        => int.TryParse(FileServiceGenerateFileMetadataThreadCount, out var r)
        ? r : SystemSettingsProxy.DefaultValues.FileServiceGenerateFileMetadataThreadCount;

    public void InsertOrUpdate(SystemSettingsProxy proxy)
    {
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
        proxy.FileServiceGenerateFileMetadataThreadCount = Math.Min(
            int.Parse(FileServiceGenerateFileMetadataThreadCount),
            SystemSettingsProxy.DefaultValues.FileServiceGenerateFileMetadataThreadCount);
        proxy.FileServicePlainTextPreviewSizeLimit = SizeHelper.ParseToByteCount(FileServicePlainTextPreviewSizeLimit);
        proxy.FileServicePlainTextPreviewSizeLimitEnforce = FileServicePlainTextPreviewSizeLimitEnforce;
        proxy.FileServiceAllowPlainTextPreview = FileServiceAllowPlainTextPreview;
    }

    public void Read(SystemSettingsProxy proxy)
    {
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
        FileServiceGenerateFileMetadataThreadCount = proxy.FileServiceGenerateFileMetadataThreadCount.ToString();
        FileServicePlainTextPreviewSizeLimit = proxy.FileServicePlainTextPreviewSizeLimit?.ToString() ?? "";
        FileServicePlainTextPreviewSizeLimitEnforce = proxy.FileServicePlainTextPreviewSizeLimitEnforce;
        FileServiceAllowPlainTextPreview = proxy.FileServiceAllowPlainTextPreview;
    }
}