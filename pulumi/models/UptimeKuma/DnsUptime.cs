using System.Collections.Immutable;

namespace applications.Models.UptimeKuma;

public class DnsUptime : UptimeBase
{

  public override string Type { get; } = "dns";
  [YamlDotNet.Serialization.YamlMember(Alias = "hostname")]
  [System.Text.Json.Serialization.JsonPropertyName("hostname")]
  public string Hostname { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "dns_resolve_server")]
  [System.Text.Json.Serialization.JsonPropertyName("dns_resolve_server")]
  public string DnsResolveServer { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "dns_resolve_type")]
  [System.Text.Json.Serialization.JsonPropertyName("dns_resolve_type")]
  public string DnsResolveType { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "port")]
  [System.Text.Json.Serialization.JsonPropertyName("port")]
  public int? Port { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "accepted_statuscodes")]
  [System.Text.Json.Serialization.JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }

}
