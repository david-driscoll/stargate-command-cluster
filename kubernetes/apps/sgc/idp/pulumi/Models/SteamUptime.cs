using System.Collections.Immutable;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace authentik.Models;

public class SteamUptime : UptimeBase
{
  public override string Type { get; } = "steam";
  [YamlMember(Alias = "hostname")]
  [JsonPropertyName("hostname")]
  public string Hostname { get; set; }
  [YamlMember(Alias = "port")]
  [JsonPropertyName("port")]
  public int? Port { get; init; }
  [YamlMember(Alias = "accepted_statuscodes")]
  [JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }

}
