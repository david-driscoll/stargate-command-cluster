using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace authentik.Models
{
  public class ApplicationDefinition : KubernetesObject, IMetadata<V1ObjectMeta>
  {
    [JsonPropertyName("metadata")]
    public V1ObjectMeta Metadata { get; set; }

    [JsonPropertyName("spec")]
    public ApplicationDefinitionSpec Spec { get; set; }

    [JsonPropertyName("status")]
    public ApplicationDefinitionStatus Status { get; set; }
  }

  // Example for one uptime type, others follow similar pattern
}
