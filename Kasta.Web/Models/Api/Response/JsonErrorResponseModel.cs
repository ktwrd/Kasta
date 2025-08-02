using System.Text.Json.Serialization;

namespace Kasta.Web.Models.Api.Response;

public class JsonErrorResponseModel
{
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
}