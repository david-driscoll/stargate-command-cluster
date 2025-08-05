using System.Collections.Immutable;

namespace authentik.Models;

public record PostgresUptime : UptimeBase
{
  public override string Type { get; } = "postgres";
  public string DatabaseConnectionString { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
}