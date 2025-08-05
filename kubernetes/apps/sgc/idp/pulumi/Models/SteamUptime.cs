using System.Collections.Immutable;

namespace authentik.Models;

public record SteamUptime : UptimeBase
{
  public override string Type { get; } = "steam";
  public string Hostname { get; init; }
  public int? Port { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

}