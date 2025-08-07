using System.Text.Json.Serialization;

namespace authentik.Models;

public record ApplicationDefinitionSpec
{
  [JsonPropertyName("name")] public string Name { get; set; }

  [JsonPropertyName("slug")] public string? Slug { get; set; }
  [JsonPropertyName("icon")] public string? Icon { get; set; }
  [JsonPropertyName("url")] public string? Url { get; set; }

  [JsonPropertyName("description")] public string? Description { get; set; }

  [JsonPropertyName("category")] public string Category { get; set; }

  [JsonPropertyName("uptime")] public ApplicationDefinitionUptime? Uptime { get; set; }

  [JsonPropertyName("uptimeFrom")] public UptimeFrom? UptimeFrom { get; set; }

  [JsonPropertyName("authentik")] public ApplicationDefinitionAuthentik? Authentik { get; set; }

  [JsonPropertyName("authentikFrom")] public AuthentikFrom? AuthentikFrom { get; init; }
}
