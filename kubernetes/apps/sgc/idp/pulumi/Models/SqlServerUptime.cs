using System.Collections.Immutable;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace authentik.Models;

public class SqlServerUptime : UptimeBase
{
  public override string Type { get; } = "sqlserver";
  [YamlMember(Alias = "database_connection_string")]
  [JsonPropertyName("database_connection_string")]
  public string DatabaseConnectionString { get; set; }
  [YamlMember(Alias = "accepted_statuscodes")]
  [JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }

}
