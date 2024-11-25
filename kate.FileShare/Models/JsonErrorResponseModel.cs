using System.Text.Json.Serialization;

namespace kate.FileShare.Models;

public class JsonErrorResponseModel
{
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
}