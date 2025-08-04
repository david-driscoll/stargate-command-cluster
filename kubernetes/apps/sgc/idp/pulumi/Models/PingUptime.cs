using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record PingUptime : UptimeBase
{
  public override string Type { get; } = "ping";
  public string Hostname { get; init; }
  public int? PacketSize { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
}