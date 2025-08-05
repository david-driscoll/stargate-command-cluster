using System.Collections.Immutable;

namespace authentik.Models;

public record MysqlUptime : UptimeBase
{
  public override string Type { get; } = "mqtt";
  public string DatabaseConnectionString { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
}