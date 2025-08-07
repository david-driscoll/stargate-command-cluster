using System.Collections.Immutable;

namespace authentik.Models;

public class PortUptime : UptimeBase
{
  public override string Type { get; } = "port";
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
