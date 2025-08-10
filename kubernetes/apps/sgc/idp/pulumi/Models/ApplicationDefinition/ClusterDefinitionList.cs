using System.Text.Json.Serialization;
using k8s;
using k8s.Models;
using YamlDotNet.Serialization;

namespace Models.ApplicationDefinition;

public class ClusterDefinitionList : KubernetesObject, IMetadata<V1ListMeta>, IKubernetesList<ClusterDefinition>
{
  public V1ListMeta Metadata { get; set; }

  [YamlMember(Alias = "items")]
  [JsonPropertyName("items")]
  public List<ClusterDefinition> Items { get; set; }
}
