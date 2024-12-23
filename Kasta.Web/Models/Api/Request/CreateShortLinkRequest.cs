using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Kasta.Web.Models;

public class CreateShortLinkRequest
{
    [JsonPropertyName("vanity")]
    public string? Vanity { get; set; }

    [Required]
    [JsonRequired]
    [JsonPropertyName("destination")]
    public string Destination { get; set; } = "";
}