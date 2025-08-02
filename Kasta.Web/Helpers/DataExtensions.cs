using System.Text;
using Kasta.Data;
using Kasta.Data.Models;
using Kasta.Shared;
using Kasta.Web.Models;

namespace Kasta.Web.Helpers;

public static class DataExtensions
{

    public static SystemSettingsParams GetSystemSettings(this ApplicationDbContext context)
    {
        var instance = new SystemSettingsParams();
        instance.Get(context);
        return instance;
    }
    private static string GetFileUrlId(FileModel file)
    {
        return string.IsNullOrEmpty(file.ShortUrl)
            ? file.Id
            : file.ShortUrl;
    }
    private static string GetLinkUrlId(ShortLinkModel link)
    {
        return string.IsNullOrEmpty(link.ShortLink)
            ? link.Id
            : link.ShortLink;
    }
    public static string GetDownloadUrl(this FileModel file)
    {
        var fileUrlId = GetFileUrlId(file);
        var result = $"/f/{fileUrlId}";
        var endpoint = FeatureFlags.Endpoint;
        return string.IsNullOrEmpty(endpoint)
            ? result
            : CombineUrl(endpoint, result);
    }
    public static string? GetPreviewUrl(this FileModel file)
    {
        if (file.Preview != null && file.Preview.RelativeLocation != file.RelativeLocation)
        {
            return file.GetDownloadUrl() + "?preview=true";
        }
        return null;
    }
    public static string GetDetailsUrl(this FileModel file)
    {
        var fileUrlId = GetFileUrlId(file);
        var result = $"/d/{fileUrlId}";
        var endpoint = FeatureFlags.Endpoint;
        if (string.IsNullOrEmpty(endpoint))
        {
            return result;
        }

        return CombineUrl(endpoint, result);
    }
    public static string GetUrl(this ShortLinkModel link)
    {
        var urlId = GetLinkUrlId(link);
        var result = $"/l/{urlId}";
        var endpoint = FeatureFlags.Endpoint;
        if (string.IsNullOrEmpty(endpoint))
        {
            return result;
        }

        return CombineUrl(endpoint, result);
    }

    private static string CombineUrl(string endpoint, string relative)
    {
        var baseUri = new Uri(endpoint);
        if (Uri.TryCreate(baseUri, relative, out var u))
        {
            return u.ToString();
        }
        
        var x = baseUri.ToString();
        while (x.EndsWith('/'))
        {
            x = x.Substring(0, x.Length - 1);
        }
        var sb = new StringBuilder();
        sb.Append(x);
        if (!relative.StartsWith('/'))
        {
            sb.Append('/');
        }
        sb.Append(relative);
        return sb.ToString();
    }
}