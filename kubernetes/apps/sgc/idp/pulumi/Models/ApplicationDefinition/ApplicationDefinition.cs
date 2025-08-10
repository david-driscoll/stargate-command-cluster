using System.Text.Json.Serialization;
using k8s;
using k8s.Models;
using YamlDotNet.Serialization;

namespace Models.ApplicationDefinition;

public class ApplicationDefinition : KubernetesObject, IMetadata<V1ObjectMeta>
{
  [YamlMember(Alias = "metadata")]
  [JsonPropertyName("metadata")]
  public V1ObjectMeta Metadata { get; set; }

  [YamlMember(Alias = "spec")]
  [JsonPropertyName("spec")]
  public ApplicationDefinitionSpec Spec { get; set; }

  [YamlMember(Alias = "status")]
  [JsonPropertyName("status")]
  public ApplicationDefinitionStatus Status { get; set; }
}

// Example for one uptime type, others follow similar pattern
