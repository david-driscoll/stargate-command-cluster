using System.Collections.Immutable;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace applications.Models.UptimeKuma;

public class MqttUptime : UptimeBase
{
  public override string Type { get; } = "mqtt";
  [YamlMember(Alias = "mqtt_check_type")]
  [JsonPropertyName("mqtt_check_type")]
  public string MqttCheckType { get; set; }
  [YamlMember(Alias = "mqtt_username")]
  [JsonPropertyName("mqtt_username")]
  public string MqttUsername { get; set; }
  [YamlMember(Alias = "mqtt_password")]
  [JsonPropertyName("mqtt_password")]
  public string MqttPassword { get; set; }

  [YamlMember(Alias = "mqtt_topic")]
  [JsonPropertyName("mqtt_topic")]
  public string MqttTopic { get; set; }
  [YamlMember(Alias = "mqtt_success_message")]
  [JsonPropertyName("mqtt_success_message")]
  public string MqttSuccessMessage { get; init; }

  [YamlMember(Alias = "hostname")]
  [JsonPropertyName("hostname")]
  public string Hostname { get; init; }
  [YamlMember(Alias = "port")]
  [JsonPropertyName("port")]
  public int? Port { get; init; }
  [YamlMember(Alias = "accepted_statuscodes")]
  [JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
}
