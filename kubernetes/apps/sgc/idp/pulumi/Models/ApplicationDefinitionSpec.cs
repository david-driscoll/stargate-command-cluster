using System.Text.Json.Serialization;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record ApplicationDefinitionSpec
{
  [JsonPropertyName("name")] public string Name { get; init; }

  [JsonPropertyName("slug")] public string? Slug { get; init; }
  [JsonPropertyName("icon")] public string? Icon { get; init; }
  [JsonPropertyName("url")] public string? Url { get; init; }

  [JsonPropertyName("description")] public string? Description { get; init; }

  [JsonPropertyName("category")] public string Category { get; init; }

  [JsonPropertyName("uptime")] public ApplicationDefinitionUptime? Uptime { get; init; }

  [JsonPropertyName("uptimeFrom")] public UptimeFrom? UptimeFrom { get; init; }

  [JsonPropertyName("authentik")] public ApplicationDefinitionAuthentik? Authentik { get; init; }

  [JsonPropertyName("authentikFrom")] public AuthentikFrom? AuthentikFrom { get; init; }
}
