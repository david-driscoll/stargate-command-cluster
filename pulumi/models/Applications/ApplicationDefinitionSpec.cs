using System.Collections.Immutable;
using System.Text.Json.Serialization;
using applications.Models.Authentik;
using applications.Models.UptimeKuma;
using YamlDotNet.Serialization;

namespace models.Applications;

public record ApplicationDefinitionSpec
{
  [YamlMember(Alias = "name")]
  [JsonPropertyName("name")]
  public string Name { get; set; }

  [YamlMember(Alias = "slug")]
  [JsonPropertyName("slug")]
  public string? Slug { get; set; }

  [YamlMember(Alias = "icon")]
  [JsonPropertyName("icon")]
  public string? Icon { get; set; }

  [YamlMember(Alias = "url")]
  [JsonPropertyName("url")]
  public string? Url { get; set; }

  [YamlMember(Alias = "description")]
  [JsonPropertyName("description")]
  public string? Description { get; set; }

  [YamlMember(Alias = "category")]
  [JsonPropertyName("category")]
  public string Category { get; set; }

  [YamlMember(Alias = "access_policy")]
  [JsonPropertyName("access_policy")]
  public ApplicationDefinitionAccessPolicy? AccessPolicy { get; set; }

  [YamlMember(Alias = "uptime")]
  [JsonPropertyName("uptime")]
  public ApplicationDefinitionUptime? Uptime { get; set; }

  [YamlMember(Alias = "uptimeFrom")]
  [JsonPropertyName("uptimeFrom")]
  public UptimeFrom? UptimeFrom { get; set; }

  [YamlMember(Alias = "authentik")]
  [JsonPropertyName("authentik")]
  public ApplicationDefinitionAuthentik? Authentik { get; set; }

  [YamlMember(Alias = "authentikFrom")]
  [JsonPropertyName("authentikFrom")]
  public AuthentikFrom? AuthentikFrom { get; init; }
}

public class ApplicationDefinitionAccessPolicy
{
  public ImmutableList<string> Entitlements { get; set; } = ImmutableList<string>.Empty;
  public ImmutableList<string> Groups { get; set; } = ImmutableList<string>.Empty;
}
