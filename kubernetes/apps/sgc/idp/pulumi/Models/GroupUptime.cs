using System.Collections.Immutable;

namespace authentik.Models;

public record GroupUptime : UptimeBase
{
  public override string Type { get; } = "group";
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
}