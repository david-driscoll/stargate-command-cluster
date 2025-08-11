using System.Text.Json.Serialization;
using k8s;
using k8s.Models;
using YamlDotNet.Serialization;

namespace Models.ApplicationDefinition;

public class ClusterDefinition : KubernetesObject, IMetadata<V1ObjectMeta>
{
  [JsonPropertyName("metadata")]
  public V1ObjectMeta Metadata { get; set; }

  [JsonPropertyName("spec")]
  public ClusterDefinitionSpec Spec { get; set; }

  [JsonPropertyName("status")]
  public ClusterDefinitionStatus Status { get; set; }
}
