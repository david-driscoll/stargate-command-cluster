using System.Text.Json.Serialization;
using k8s;
using k8s.Models;
using YamlDotNet.Serialization;

namespace Models.UptimeKuma.Resources;

public class KumaResource : KubernetesObject, IMetadata<V1ObjectMeta>
{
  public V1ObjectMeta Metadata { get; set; }

  [YamlMember(Alias = "spec")]
  [JsonPropertyName("spec")]
  public object Spec { get; set; }

  [YamlMember(Alias = "status")]
  [JsonPropertyName("status")]
  public object Status { get; set; }
}
