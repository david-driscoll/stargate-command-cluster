using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record RadiusUptime : UptimeBase
{
  public override string Type { get; } = "radius";
  public string Hostname { get; init; }
  public int? Port { get; init; }
  public string RadiusCalledStationId { get; init; }
  public string RadiusCallingStationId { get; init; }
  public string RadiusPassword { get; init; }
  public string RadiusSecret { get; init; }
  public string RadiusUsername { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
}