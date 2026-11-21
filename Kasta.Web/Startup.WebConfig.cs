using System.IO.Compression;
using System.Net;
using Kasta.Shared;
using Kasta.Web.Services;
using Microsoft.AspNetCore.ResponseCompression;
using Vivet.AspNetCore.RequestTimeZone;

namespace Kasta.Web;

partial class Startup
{
    private static void ConfigureCompression(IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
                "application/json", "application/geo+json",
                "application/json", "application/pdf",
                "application/sql", "application/toml",
                "application/octet-stream", "application/xml",
                "application/wasm",
                "application/font-woff2",

                "audio/aac", "audio/flac",
                "audio/mp4", "audio/mpeg",
                "audio/ogg", "audio/opus",
                "audio/vorbis",
                "audio/midi", "audio/x-midi",

                "image/apng", "image/avif", "image/bmp",
                "image/gif", "image/heic", "image/heic-sequence",
                "image/heif", "image/heif-sequence",
                "image/jpeg", "image/jxl", "image/png",
                "image/svg+xml", "image/tiff", "image/tiff-fx",
                "image/webp", "image/vnd.microsoft.icon",

                "model/mtl", "model/mesh", "model/obj",

                "application/font-woff2",
                "font/collection", "font/otf",
                "font/sfnt", "font/ttf",
                "font/woff", "font/woff2",

                "text/javascript", "text/css",
                "text/css", "text/rtf",
                "text/plain", "text/xml",
                "text/markdown",


                "video/mp4", "video/av1",
                "video/mpeg", "video/mpeg",
                "video/ogg", "video/quicktime",
                "video/webm"
            ]);
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.SmallestSize;
        });
    }

    private static void ConfigureForwardedHeadersOptions(IServiceCollection services)
    {
        var cfg = KastaConfig.Instance;
        var parsedProxyAddresses = new List<IPAddress>();
        var ipAddressValueMapping = new List<(string, IPAddress)>()
        {
            ("any", IPAddress.Any),
            ("loopback", IPAddress.Loopback),
            ("localhost", IPAddress.Loopback),
            ("ipv6any", IPAddress.IPv6Any),
            ("ipv6loopback", IPAddress.IPv6Loopback),
        };
        foreach (var addr in cfg.Proxy?.KnownProxies.Distinct() ?? [])
        {
            var altTarget = ipAddressValueMapping
                .Where(e => e.Item1.Equals(addr, StringComparison.InvariantCultureIgnoreCase))
                .Select(e => e.Item2)
                .FirstOrDefault();
            if (altTarget != null)
            {
                parsedProxyAddresses.Add(altTarget);
            }
            else
            {
                if (!IPAddress.TryParse(addr, out var ipAddr))
                {
                    throw new InvalidOperationException($"Invalid IP Address format for Known Proxy address: \"{addr}\"");
                }
                parsedProxyAddresses.Add(ipAddr);
            }
        }
        services.Configure<ForwardedHeadersOptions>(opts =>
        {
            if (cfg.Proxy == null) return;
            foreach (var a in parsedProxyAddresses) opts.KnownProxies.Add(a);
            if (cfg.Proxy.ForwardedHeaders.HasValue)
            {
                opts.ForwardedHeaders = cfg.Proxy.ForwardedHeaders.Value;
            }
            if (cfg.Proxy.ForwardLimit.HasValue)
            {
                opts.ForwardLimit = cfg.Proxy.ForwardLimit.Value;
            }
            if (cfg.Proxy.ForwardedForHeaderName != null)
            {
                opts.ForwardedForHeaderName = cfg.Proxy.ForwardedForHeaderName;
            }
            if (cfg.Proxy.ForwardedProtoHeaderName != null)
            {
                opts.ForwardedProtoHeaderName = cfg.Proxy.ForwardedProtoHeaderName;
            }
        });
    }

    private static void ConfigureTimeZone(RequestTimeZoneOptions options)
    {
        var cfg = KastaConfig.Instance;
        options.DefaultTimeZone = string.IsNullOrEmpty(cfg.DefaultTimezone?.Trim())
            ? "UTC"
            : cfg.DefaultTimezone;
        options.RequestTimeZoneProviders.Add(new IpAddressRequestTimeZoneProvider());
    }
}