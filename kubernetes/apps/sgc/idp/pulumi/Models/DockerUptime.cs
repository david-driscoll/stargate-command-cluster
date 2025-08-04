using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record DockerUptime : UptimeBase
{
  public override string Type { get; } = "docker";
  public string DockerContainer { get; init; }
  public string DockerHost { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
}