using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace models.Applications;

public class ClusterDefinition : KubernetesObject, IMetadata<V1ObjectMeta>
{
  public V1ObjectMeta Metadata { get; set; }

  [JsonPropertyName("spec")]
  public ClusterDefinitionSpec Spec { get; set; }

  [JsonPropertyName("status")]
  public ClusterDefinitionStatus Status { get; set; }
}
