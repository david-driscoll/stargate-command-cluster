using System.Text.Json.Serialization;
using k8s;
using k8s.Models;
using YamlDotNet.Serialization;

namespace Models.UptimeKuma.Resources;

public class KumaResourceList : KubernetesObject, IMetadata<V1ListMeta>, IKubernetesList<KumaResource>
{
  public V1ListMeta Metadata { get; set; }

  [YamlMember(Alias = "items")]
  [JsonPropertyName("items")]
  public List<KumaResource> Items { get; set; }
}
