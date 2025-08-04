using System.Text.Json.Serialization;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record AuthentikFrom
{
  [JsonPropertyName("type")]
  public string Type { get; init; }
  [JsonPropertyName("name")]
  public string Name { get; init; }
}