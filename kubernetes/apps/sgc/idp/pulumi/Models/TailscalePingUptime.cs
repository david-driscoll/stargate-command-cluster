using System.Collections.Immutable;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace authentik.Models;

public class TailscalePingUptime : UptimeBase
{
  public override string Type { get; } = "tailscale-ping";
  [YamlMember(Alias = "hostname")]
  [JsonPropertyName("hostname")]
  public string Hostname { get; set; }
  [YamlMember(Alias = "accepted_statuscodes")]
  [JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }

}
