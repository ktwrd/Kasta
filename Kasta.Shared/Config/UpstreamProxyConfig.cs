using System.Xml.Serialization;
using Microsoft.AspNetCore.HttpOverrides;

namespace Kasta.Shared;

public class UpstreamProxyConfig
{
    /// <summary>
    /// Known/trusted proxy for ASP.NET.
    /// </summary>
    /// <remarks>
    /// The following value mappings exist (invariant culture, ignore case):
    /// <list type="bullet">
    /// <item><c>any</c> = <see cref="System.Net.IPAddress.Any"/></item>
    /// <item><c>loopback</c> = <see cref="System.Net.IPAddress.Loopback"/></item>
    /// <item><c>locahost</c> = <see cref="System.Net.IPAddress.Loopback"/></item>
    /// <item><c>ipv6any</c> = <see cref="System.Net.IPAddress.IPv6Any"/></item>
    /// <item><c>ipv6loopback</c> = <see cref="System.Net.IPAddress.IPv6Loopback"/></item>
    /// </list>
    /// </remarks>
    [XmlElement("KnownProxy")]
    public List<string> KnownProxies { get; set; } = [];

    /// <summary>
    /// Known/trusted networks for ASP.NET.
    /// </summary>
    [XmlElement("KnownNetwork")]
    public List<string> KnownNetworks { get; set; } = [];
    
    /// <summary>
    /// Forwarded Headers to use for ASP.NET. Ignored when <see langword="null"/>
    /// </summary>
    [XmlElement("ForwardedHeaders")]
    public ForwardedHeaders? ForwardedHeaders { get; set; }

    [XmlElement("ForwardLimit")]
    public int? ForwardLimit { get; set; }
    
    /// <summary>
    /// Header Name that is used to fetch the real IP Address from the upstream proxy.
    /// </summary>
    [XmlElement("ForwardedForHeaderName")]
    public string? ForwardedForHeaderName
    {
        get;
        set => field = string.IsNullOrEmpty(value?.Trim()) ? null : value.Trim();
    }

    [XmlElement("ForwardedProtoHeaderName")]
    public string? ForwardedProtoHeaderName
    {
        get;
        set => field = string.IsNullOrEmpty(value?.Trim()) ? null : value.Trim();
    }
    
    /// <summary>
    /// <para>Path Base to prepend to the URL. Useful if Kasta is available at something like <c>https://app.example.com/kasta</c></para>
    /// If <c>/foo</c> is the app base path for a proxy path passed as <c>/foo/api/1</c>, the middleware sets Request.PathBase to <c>/foo</c> and Request.Path to <c>/api/1</c>
    /// </summary>
    /// <remarks>
    /// See this document for more information. Kasta calls <c>app.UsePathBase</c>
    /// <see href="https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0#work-with-path-base-and-proxies-that-change-the-request-path"/>
    /// </remarks>
    [XmlElement("PathBase")]
    public string? PathBase
    {
        get;
        set => field = string.IsNullOrEmpty(value?.Trim()) ? null : value.Trim();
    }
    
    /// <summary>
    /// Append <see cref="PathBase"/> to the Request Path if it isn't already prepended by the proxy.
    /// </summary>
    [XmlElement("IsProxyPrependingPathBase")]
    public bool IsProxyPrependingPathBase { get; set; }
    
    /// <summary>
    /// If the proxy trims the path (for example, forwarding <c>/foo/api/1</c> to <c>/api/1</c>), then enable this.
    /// </summary>
    public bool IsProxyTrimmingPathBase { get; set; }
}