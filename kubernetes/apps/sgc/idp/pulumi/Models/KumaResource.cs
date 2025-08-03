using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

public class KumaResource : KubernetesObject, IMetadata<V1ObjectMeta>
{
  [JsonPropertyName("metadata")]
  public V1ObjectMeta Metadata { get; set; }

  [JsonPropertyName("spec")]
  public object Spec { get; set; }

  [JsonPropertyName("status")]
  public object Status { get; set; }
}