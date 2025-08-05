using System.Text.Json.Serialization;

namespace authentik.Models;

public record AuthentikFrom
{
  [JsonPropertyName("type")]
  public string Type { get; init; }
  [JsonPropertyName("name")]
  public string Name { get; init; }
}
