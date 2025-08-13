using System.Collections.Immutable;

namespace applications.Models.UptimeKuma;

public class RedisUptime : UptimeBase
{
  public override string Type { get; } = "redis";
  [YamlDotNet.Serialization.YamlMember(Alias = "database_connection_string")]
  [System.Text.Json.Serialization.JsonPropertyName("database_connection_string")]
  public string DatabaseConnectionString { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "accepted_statuscodes")]
  [System.Text.Json.Serialization.JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
}
