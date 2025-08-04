using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record GroupUptime : UptimeBase
{
  public override string Type { get; } = "group";
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
}