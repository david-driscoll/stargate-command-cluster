using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Models;

public interface IKubernetesList<T>
{
  [YamlMember(Alias = "items")]
  [JsonPropertyName("items")]
  public List<T> Items { get; set; }
}
