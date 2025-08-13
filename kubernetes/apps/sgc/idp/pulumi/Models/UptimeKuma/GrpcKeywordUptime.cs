using System.Collections.Immutable;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace applications.Models.UptimeKuma;

public class GrpcKeywordUptime : UptimeBase
{
  public override string Type { get; } = "grpc-keyword";
  [YamlMember(Alias = "grpc_body")]
  [JsonPropertyName("grpc_body")]
  public string GrpcBody { get; set; }
  [YamlMember(Alias = "grpc_enable_tls")]
  [JsonPropertyName("grpc_enable_tls")]
  public bool? GrpcEnableTls { get; set; }
  [YamlMember(Alias = "grpc_metadata")]
  [JsonPropertyName("grpc_metadata")]
  public string GrpcMetadata { get; set; }
  [YamlMember(Alias = "grpc_method")]
  [JsonPropertyName("grpc_method")]
  public string GrpcMethod { get; set; }
  [YamlMember(Alias = "grpc_protobuf")]
  [JsonPropertyName("grpc_protobuf")]
  public string GrpcProtobuf { get; set; }
  [YamlMember(Alias = "grpc_service_name")]
  [JsonPropertyName("grpc_service_name")]
  public string GrpcServiceName { get; set; }
  [YamlMember(Alias = "grpc_url")]
  [JsonPropertyName("grpc_url")]
  public string GrpcUrl { get; set; }
  [YamlMember(Alias = "invert_keyword")]
  [JsonPropertyName("invert_keyword")]
  public bool? InvertKeyword { get; init; }
  [YamlMember(Alias = "keyword")]
  [JsonPropertyName("keyword")]
  public string Keyword { get; init; }
  [YamlMember(Alias = "accepted_statuscodes")]
  [JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }

}
