using System.Collections.Immutable;

namespace Models.UptimeKuma;

public class PostgresUptime : UptimeBase
{
  public override string Type { get; } = "postgres";
  [YamlDotNet.Serialization.YamlMember(Alias = "database_connection_string")]
  [System.Text.Json.Serialization.JsonPropertyName("database_connection_string")]
  public string DatabaseConnectionString { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "accepted_statuscodes")]
  [System.Text.Json.Serialization.JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
}
