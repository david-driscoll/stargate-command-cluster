using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record TailscalePingUptime : UptimeBase
{
  public override string Type { get; } = "tailscale-ping";
  public string Hostname { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

}