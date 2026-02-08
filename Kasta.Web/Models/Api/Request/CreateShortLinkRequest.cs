using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Kasta.Web.Models.Api.Request;

public class CreateShortLinkRequest
{
    [JsonPropertyName("vanity")]
    public string? Vanity
    {
        get;
        set => field = string.IsNullOrEmpty(value?.Trim()) ? null : value.Trim();
    }

    [Required]
    [JsonRequired]
    [JsonPropertyName("destination")]
    public string Destination
    {
        get;
        set => field = value.Trim();
    } = "";
}