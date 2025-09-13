using System.Collections.Generic;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace models.Applications;

public class ClusterDefinitionList : KubernetesObject, IKubernetesObject<V1ListMeta>, IKubernetesList<ClusterDefinition>
{
  public V1ListMeta Metadata { get; set; }

  [JsonPropertyName("items")]
  public List<ClusterDefinition> Items { get; set; }
}
