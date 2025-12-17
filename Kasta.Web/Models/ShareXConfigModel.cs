using System.Text.Json.Serialization;

namespace Kasta.Web.Models;

public class ShareXConfigModel
{
    [JsonPropertyName("DestinationType")]
    public string? DestinationType { get; set; }

    [JsonPropertyName("RequestURL")]
    public required string RequestUrl { get; set; }

    [JsonPropertyName("FileFormName")]
    public string? FileFormName { get; set; }

    [JsonPropertyName("Arguments")]
    public Dictionary<string, object>? Arguments { get; set; }

    [JsonPropertyName("URL")]
    public required string Url { get; set; }

    [JsonPropertyName("ThumbnailURL")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("DeletionURL")]
    public string? DeletionUrl { get; set; }
}