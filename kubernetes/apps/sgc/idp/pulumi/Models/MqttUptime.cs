using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record MqttUptime : UptimeBase
{
  public override string Type { get; } = "mqtt";
  public string MqttCheckType { get; init; }
  public string MqttUsername { get; init; }
  public string MqttPassword { get; init; }
  public string MqttTopic { get; init; }
  public string MqttSuccessMessage { get; init; }
  public string Hostname { get; init; }
  public int? Port { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
}