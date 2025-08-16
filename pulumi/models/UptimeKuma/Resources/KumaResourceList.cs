using System.Collections.Generic;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;
using models;

namespace applications.Models.UptimeKuma.Resources;

public class KumaResourceList : KubernetesObject, IMetadata<V1ListMeta>, IKubernetesList<KumaResource>
{
  public V1ListMeta Metadata { get; set; }

  [JsonPropertyName("items")]
  public List<KumaResource> Items { get; set; }
}
