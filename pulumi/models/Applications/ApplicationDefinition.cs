using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace models.Applications;

public class ApplicationDefinition : KubernetesObject, IMetadata<V1ObjectMeta>, IKubernetesSpec
{
  [JsonPropertyName("metadata")]
  public V1ObjectMeta Metadata { get; set; }

  [JsonPropertyName("spec")]
  public ApplicationDefinitionSpec Spec { get; set; }

  [JsonPropertyName("status")]
  public ApplicationDefinitionStatus Status { get; set; }

  object IKubernetesSpec.Spec => Spec;
}

// Example for one uptime type, others follow similar pattern
