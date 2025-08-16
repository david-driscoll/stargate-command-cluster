using System.Collections.Generic;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace models.Applications;

public class ApplicationDefinitionList : KubernetesObject, IMetadata<V1ListMeta>, IKubernetesList<ApplicationDefinition>
{
  public V1ListMeta Metadata { get; set; }

  [JsonPropertyName("items")]
  public List<ApplicationDefinition> Items { get; set; }
}
