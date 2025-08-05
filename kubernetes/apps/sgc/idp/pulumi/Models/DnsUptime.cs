using System.Collections.Immutable;

namespace authentik.Models;

public record DnsUptime : UptimeBase
{

  public override string Type { get; } = "dns";
  public string Hostname { get; init; }
  public string DnsResolveServer { get; init; }
  public string DnsResolveType { get; init; }
  public int? Port { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

}