using System.Collections.Immutable;

namespace authentik.Models;

public record PortUptime : UptimeBase
{
  public override string Type { get; } = "port";
  public string Hostname { get; init; }
  public int? Port { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

}