using System.Collections.Immutable;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace authentik.Models;

public class MqttUptime : UptimeBase
{
  public override string Type { get; } = "mqtt";
  public string MqttCheckType { get; set; }
  public string MqttUsername { get; set; }
  public string MqttPassword { get; set; }
  public string MqttTopic { get; set; }
  public string MqttSuccessMessage { get; init; }
  public string Hostname { get; init; }
  public int? Port { get; init; }
  [YamlMember(Alias = "accepted_statuscodes")]
  [JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
}
