using System.Collections.Immutable;

namespace authentik.Models;

public record SqlServerUptime : UptimeBase
{
  public override string Type { get; } = "sqlserver";
  public string DatabaseConnectionString { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

}