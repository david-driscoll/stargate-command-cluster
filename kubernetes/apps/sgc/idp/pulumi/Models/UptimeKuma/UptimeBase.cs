using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Models.UptimeKuma;

public abstract class UptimeBase
{
  [YamlMember(Alias = "type")]
  [JsonPropertyName("type")]
  public abstract string Type { get; }

  [YamlMember(Alias = "active")]
  [JsonPropertyName("active")]
  public bool? Active { get; set; } = true;

  [YamlMember(Alias = "interval")]
  [JsonPropertyName("interval")]
  public int? Interval { get; set; } = 300;

  [YamlMember(Alias = "max_retries")]
  [JsonPropertyName("max_retries")]
  public int? MaxRetries { get; set; } = 3;

  [YamlMember(Alias = "parent_name")]
  [JsonPropertyName("parent_name")]
  public string? ParentName { get; set; }

  [YamlMember(Alias = "retry_interval")]
  [JsonPropertyName("retry_interval")]
  public int? RetryInterval { get; set; } = 60;


  [YamlMember(Alias = "upside_down")]
  [JsonPropertyName("upside_down")]
  public bool UpsideDown { get; set; }
}
