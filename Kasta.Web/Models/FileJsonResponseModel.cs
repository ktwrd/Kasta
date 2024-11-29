using System.Text.Json.Serialization;

namespace Kasta.Web.Models;

public class FileJsonResponseModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("urlDetail")]
    public string DetailsUrl { get; set; }
    [JsonPropertyName("urlDelete")]
    public string DeleteUrl { get; set; }
    [JsonPropertyName("filename")]
    public string Filename { get; set; }
    [JsonPropertyName("size")]
    public long FileSize { get; set; }
    [JsonPropertyName("createdAt")]
    public long CreatedAtTimestamp { get; set; }
}