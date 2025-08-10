using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Models.Authentik;

public record AuthentikFrom
{
  [YamlMember(Alias = "type")]
  [JsonPropertyName("type")]
  public string Type { get; init; }

  [YamlMember(Alias = "name")]
  [JsonPropertyName("name")]
  public string Name { get; init; }
}
