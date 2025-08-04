using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record GamedigUptime : UptimeBase
{
  public override string Type { get; } = "gamedig";
  public string Game { get; init; }
  public bool? GamedigGivenPortOnly { get; init; }
  public string Hostname { get; init; }
  public int? Port { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;


}