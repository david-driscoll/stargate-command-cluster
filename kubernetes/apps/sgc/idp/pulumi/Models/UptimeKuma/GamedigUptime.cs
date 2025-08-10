using System.Collections.Immutable;

namespace Models.UptimeKuma;

public class GamedigUptime : UptimeBase
{
  public override string Type { get; } = "gamedig";
  [YamlDotNet.Serialization.YamlMember(Alias = "game")]
  [System.Text.Json.Serialization.JsonPropertyName("game")]
  public string Game { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "gamedig_given_port_only")]
  [System.Text.Json.Serialization.JsonPropertyName("gamedig_given_port_only")]
  public bool? GamedigGivenPortOnly { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "hostname")]
  [System.Text.Json.Serialization.JsonPropertyName("hostname")]
  public string Hostname { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "port")]
  [System.Text.Json.Serialization.JsonPropertyName("port")]
  public int? Port { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "accepted_statuscodes")]
  [System.Text.Json.Serialization.JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }


}
