using System.Collections.Immutable;

namespace authentik.Models;

public record RedisUptime : UptimeBase
{
  public override string Type { get; } = "redis";
  public string DatabaseConnectionString { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
}