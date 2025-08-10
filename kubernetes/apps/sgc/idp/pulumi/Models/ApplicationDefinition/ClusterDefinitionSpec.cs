using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Models.ApplicationDefinition;

public record ClusterDefinitionSpec
{

  [YamlMember(Alias = "name")]
  [JsonPropertyName("name")]
  public string Name { get; set; }

  [YamlMember(Alias = "secret")]
  [JsonPropertyName("secret")]
  public string Secret { get; set; }

  [YamlMember(Alias = "description")]
  [JsonPropertyName("description")]
  public string? Description { get; set; }

  [YamlMember(Alias = "icon")]
  [JsonPropertyName("icon")]
  public string? Icon { get; set; }
}