using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record GrpcKeywordUptime : UptimeBase
{
  public override string Type { get; } = "grpc-keyword";
  public string GrpcBody { get; init; }
  public bool? GrpcEnableTls { get; init; }
  public string GrpcMetadata { get; init; }
  public string GrpcMethod { get; init; }
  public string GrpcProtobuf { get; init; }
  public string GrpcServiceName { get; init; }
  public string GrpcUrl { get; init; }
  public bool? InvertKeyword { get; init; }
  public string Keyword { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

}