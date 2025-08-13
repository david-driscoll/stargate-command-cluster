using System.Text.Json.Serialization;

namespace applications.Models.UptimeKuma;

public record UptimeFrom
{
  [JsonPropertyName("type")]
  public string Type { get; init; }
  [JsonPropertyName("name")]
  public string Name { get; init; }
}
