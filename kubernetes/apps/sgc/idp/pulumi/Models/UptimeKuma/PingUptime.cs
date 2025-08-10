using System.Collections.Immutable;

namespace Models.UptimeKuma;

public class PingUptime : UptimeBase
{
  public override string Type { get; } = "ping";
  [YamlDotNet.Serialization.YamlMember(Alias = "hostname")]
  [System.Text.Json.Serialization.JsonPropertyName("hostname")]
  public string Hostname { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "packet_size")]
  [System.Text.Json.Serialization.JsonPropertyName("packet_size")]
  public int? PacketSize { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "accepted_statuscodes")]
  [System.Text.Json.Serialization.JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
}
