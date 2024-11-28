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
        if (string.IsNullOrEmpty(file.ShortUrl))
        {
            return file.Id;
        }
        return file.ShortUrl;
    }
    public static string GetDownloadUrl(this FileModel file)
    {
        var fileUrlId = GetFileUrlId(file);
        string result = $"/f/{fileUrlId}";
        var endpoint = FeatureFlags.Endpoint;
        if (string.IsNullOrEmpty(endpoint))
        {
            return result;
        }

        return CombineUrl(endpoint, result);
    }
    public static string? GetPreviewUrl(this FileModel file)
    {
        if (file.Preview != null && file.Preview.RelativeLocation != file.RelativeLocation)
        {
            var result = file.GetDownloadUrl();
            return result + "?preview=true";
        }
        return null;
    }
    public static string GetDetailsUrl(this FileModel file)
    {
        var fileUrlId = GetFileUrlId(file);
        string result = $"/d/{fileUrlId}";
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