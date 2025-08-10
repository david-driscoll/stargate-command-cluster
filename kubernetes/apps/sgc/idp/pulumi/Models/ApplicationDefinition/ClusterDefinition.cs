using System.Text.Json.Serialization;
using k8s;
using k8s.Models;
using YamlDotNet.Serialization;

namespace Models.ApplicationDefinition;

public class ClusterDefinition : KubernetesObject, IMetadata<V1ObjectMeta>
{
  [YamlMember(Alias = "metadata")]
  [JsonPropertyName("metadata")]
  public V1ObjectMeta Metadata { get; set; }

  [YamlMember(Alias = "spec")]
  [JsonPropertyName("spec")]
  public ClusterDefinitionSpec Spec { get; set; }

  [YamlMember(Alias = "status")]
  [JsonPropertyName("status")]
  public ClusterDefinitionStatus Status { get; set; }
}