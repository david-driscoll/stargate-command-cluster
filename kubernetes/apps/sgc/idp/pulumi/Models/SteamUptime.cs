using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record SteamUptime : UptimeBase
{
  public override string Type { get; } = "steam";
  public string Hostname { get; init; }
  public int? Port { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

}