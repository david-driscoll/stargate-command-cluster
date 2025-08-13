using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace applications.Models;

public interface IKubernetesList<T>
{
  [YamlMember(Alias = "items")]
  [JsonPropertyName("items")]
  public List<T> Items { get; set; }
}

public interface IKubernetesSpec
{
  object Spec { get; }
}
