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
}