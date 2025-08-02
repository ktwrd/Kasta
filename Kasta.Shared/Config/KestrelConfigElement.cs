using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Kasta.Shared;

public class KestrelConfigElement
{
    [XmlElement("Limits")]
    public KestrelLimitsElement? Limits { get; set; }
}

/// <summary>
/// XML Element for <c>Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits</c>
/// </summary>
public class KestrelLimitsElement
{
    /// <summary>
    /// Gets or sets the maximum size of the response buffer before write calls begin
    /// to block or return tasks that don't complete until the buffer size drops below
    /// the configured limit. Defaults to 65,536 bytes (64 KB).
    /// </summary>
    /// <remarks>
    /// When set to <c>-1</c>, the size of the response buffer is unlimited. When set to zero,
    /// all write calls will block or return tasks that don't complete until the entire
    /// response buffer is flushed.
    /// </remarks>
    [XmlElement("MaxResponseBufferSize")]
    public long? MaxResponseBufferSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum size of the request buffer.
    /// Defaults to 1,048,576 bytes (1 MB).
    /// </summary>
    /// <remarks>
    /// When <c>-1</c>, the size of the request buffer is unlimited.
    /// </remarks>
    [XmlElement("MaxRequestBufferSize")]
    public long? MaxRequestBufferSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed size for the HTTP request line.
    /// Defaults to 8,192 bytes (8 KB).
    /// </summary>
    /// <remarks>
    /// For HTTP/2 and HTTP/3 this measures the total size of the required pseudo headers
    /// :method, :scheme, :authority, and :path.
    /// </remarks>
    [XmlElement("MaxRequestLineSize")]
    public int? MaxRequestLineSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed size for the HTTP request headers.
    /// Defaults to 32,768 bytes (32 KB).
    /// </summary>
    [XmlElement("MaxRequestHeadersTotalSize")]
    public int? MaxRequestHeadersTotalSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed number of headers per HTTP request.
    ///  Defaults to 100.
    /// </summary>
    [XmlElement("MaxRequestHeaderCount")]
    public int? MaxRequestHeaderCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed size of any request body in bytes. When set
    /// to <c>-1</c>, the maximum request body size is unlimited. This limit has no effect
    /// on upgraded connections which are always unlimited. This can be overridden per-request
    /// via Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature.
    /// Defaults to 30,000,000 bytes, which is approximately 28.6MB.
    /// </summary>
    [XmlElement("MaxRequestBodySize")]
    public long? MaxRequestBodySize { get; set; }

    /// <summary>
    /// Gets or sets the keep-alive timeout.
    /// 
    /// Defaults to 130 seconds.
    /// </summary>
    [XmlElement("KeepAliveTimeout")]
    public TimeSpanElement? KeepAliveTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum amount of time the server will spend receiving request
    /// headers.
    /// 
    /// Defaults to 30 seconds.
    /// </summary>
    [XmlElement("RequestHeadersTimeout")]
    public TimeSpanElement? RequestHeadersTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of open connections. When set to <c>-1</c>, the number
    /// of connections is unlimited.
    /// 
    /// Defaults to <c>-1</c>
    /// </summary>
    /// <remarks>
    /// When a connection is upgraded to another protocol, such as WebSockets, its connection
    /// is counted against the <c>Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxConcurrentUpgradedConnections</c>
    /// limit instead of <c>Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxConcurrentConnections</c>
    /// </remarks>
    [XmlElement("MaxConcurrentConnections")]
    public long? MaxConcurrentConnections { get; set; }

    /// <summary>
    /// <para>
    /// Gets or sets the maximum number of open, upgraded connections. When set to <c>-1</c>,
    /// the number of upgraded connections is unlimited. An upgraded connection is one
    /// that has been switched from HTTP to another protocol, such as WebSockets.
    /// </para>
    /// 
    /// Defaults to <c>-1</c>
    /// </summary>
    /// <remarks>
    /// When a connection is upgraded to another protocol, such as WebSockets, its connection 
    /// is counted against the <c>Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxConcurrentUpgradedConnections</c>
    /// limit instead of <c>Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits.MaxConcurrentConnections</c>
    /// </remarks>
    [XmlElement("MaxConcurrentUpgradedConnections")]
    public long? MaxConcurrentUpgradedConnections { get; set; }

    /// <summary>
    /// <para>
    /// Gets or sets the request body minimum data rate in bytes/second. This limit
    /// has no effect on upgraded connections which are always unlimited. This can be
    /// overridden per-request via <c>Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinRequestBodyDataRateFeature</c>.
    /// </para>
    /// 
    /// Defaults to 240 bytes/second with a 5-second grace period.
    /// </summary>
    [XmlElement("MinRequestBodyDataRate")]
    public KestrelMinDataRateElement? MinRequestBodyDataRate { get; set; }

    /// <summary>
    /// When <see langword="true"/>, <see cref="MinRequestBodyDataRate"/> will not be enforced.
    /// When <see langword="null"/>, it will be enforced with <see cref="MinRequestBodyDataRate"/> 
    /// or the default value when that is <see langword="null"/>.
    /// </summary>
    [XmlElement("EnforceMinRequestBodyDataRate")]
    public bool? EnforceMinRequestBodyDataRate { get; set; }

    /// <summary>
    /// <para>
    /// Gets or sets the response minimum data rate in bytes/second. This limit has no
    /// effect on upgraded connections which are always unlimited. This can be overridden
    /// per-request via <c>Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinResponseDataRateFeature</c>
    /// </para>
    /// 
    /// Defaults to 240 bytes/second with a 5-second grace period.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contrary to the request body minimum data rate, this rate applies to the response
    /// status line and headers as well.
    /// </para>
    /// 
    /// This rate is enforced per write operation instead of being averaged over the
    /// life of the response. Whenever the server writes a chunk of data, a timer is
    /// set to the maximum of the grace period set in this property or the length of
    /// the write in bytes divided by the data rate (i.e. the maximum amount of time
    /// that write should take to complete with the specified data rate). The connection
    /// is aborted if the write has not completed by the time that timer expires.
    /// </remarks>
    [XmlElement("MinResponseDataRate")]
    public KestrelMinDataRateElement? MinResponseDataRate { get; set; }

    /// <summary>
    /// When <see langword="true"/>, <see cref="MinResponseDataRate"/> will not be enforced.
    /// When <see langword="null"/>, it will be enforced with <see cref="MinResponseDataRate"/> 
    /// or the default value when that is <see langword="null"/>.
    /// </summary>
    [XmlElement("EnforceMinResponseDataRate")]
    public bool? EnforceMinResponseDataRate { get; set; }
}

public class KestrelMinDataRateElement
{
    public KestrelMinDataRateElement()
    {
        BytesPerSecond = 240;
        GracePeriod = TimeSpanElement.FromSeconds(5);
    }
    [Required]
    [XmlElement("BytesPerSecond")]
    public double BytesPerSecond { get; set; }

    [Required]
    [XmlElement("GracePeriod")]
    public TimeSpanElement GracePeriod { get; set; }
}