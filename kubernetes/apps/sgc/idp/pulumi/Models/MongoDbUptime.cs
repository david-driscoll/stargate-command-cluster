using System.Collections.Immutable;

namespace authentik.Models;

public record MongoDbUptime : UptimeBase
{
  public override string Type { get; } = "mongodb";
  public string DatabaseConnectionString { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

}