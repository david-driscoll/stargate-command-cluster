using System.Collections.Immutable;

namespace authentik.Models;

public record PushUptime : UptimeBase
{
  public override string Type { get; } = "push";
  public string PushToken { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
}