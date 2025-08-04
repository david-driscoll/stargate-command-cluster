using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

public class KumaResourceSpec
{
  public KumaResourceConfigSpec Config { get; set; }
}
public class KumaResourceConfigSpec
{
  [JsonPropertyName("type")] public string? Type { get; set; }
  [JsonPropertyName("url")] public string? Url { get; set; }

  [JsonPropertyName("method")] public string? Method { get; set; }

  [JsonPropertyName("acceptedStatusCodes")]
  public ImmutableArray<string>? AcceptedStatusCodes { get; set; }

  [JsonPropertyName("active")] public bool? Active { get; set; }
  [JsonPropertyName("interval")] public int? Interval { get; set; }
  [JsonPropertyName("maxRedirects")] public int? MaxRedirects { get; set; }
  [JsonPropertyName("maxRetries")] public int? MaxRetries { get; set; }
  [JsonPropertyName("parentName")] public string? ParentName { get; set; }
  [JsonPropertyName("proxyId")] public string? ProxyId { get; set; }
  [JsonPropertyName("resendInterval")] public int? ResendInterval { get; set; }
  [JsonPropertyName("retryInterval")] public int? RetryInterval { get; set; }
  [JsonPropertyName("timeout")] public int? Timeout { get; set; }
  [JsonPropertyName("upsideDown")] public bool? UpsideDown { get; set; }
  [JsonPropertyName("tlsCa")] public string? TlsCa { get; set; }
  [JsonPropertyName("tlsCert")] public string? TlsCert { get; set; }
  [JsonPropertyName("tlsKey")] public string? TlsKey { get; set; }
  [JsonPropertyName("ignoreTls")] public bool? IgnoreTls { get; set; }

  [JsonPropertyName("expiryNotification")]
  public string? ExpiryNotification { get; set; }

  [JsonPropertyName("httpBodyEncoding")] public string? HttpBodyEncoding { get; set; }
  [JsonPropertyName("body")] public string? Body { get; set; }
  [JsonPropertyName("authDomain")] public string? AuthDomain { get; set; }
  [JsonPropertyName("authMethod")] public string? AuthMethod { get; set; }
  [JsonPropertyName("authWorkstation")] public string? AuthWorkstation { get; set; }
  [JsonPropertyName("basicAuthUser")] public string? BasicAuthUser { get; set; }
  [JsonPropertyName("basicAuthPass")] public string? BasicAuthPass { get; set; }
  [JsonPropertyName("oauthAuthMethod")] public string? OauthAuthMethod { get; set; }
  [JsonPropertyName("oauthClientId")] public string? OauthClientId { get; set; }

  [JsonPropertyName("oauthClientSecret")]
  public string? OauthClientSecret { get; set; }

  [JsonPropertyName("oauthScopes")] public string? OauthScopes { get; set; }
  [JsonPropertyName("oauthTokenUrl")] public string? OauthTokenUrl { get; set; }
  [JsonPropertyName("hostname")] public string? Hostname { get; set; }
  [JsonPropertyName("packetSize")] public string? PacketSize { get; set; }
  [JsonPropertyName("dockerContainer")] public string? DockerContainer { get; set; }
  [JsonPropertyName("dockerHost")] public string? DockerHost { get; set; }
  [JsonPropertyName("dnsResolveServer")] public string? DnsResolveServer { get; set; }
  [JsonPropertyName("dnsResolveType")] public string? DnsResolveType { get; set; }
  [JsonPropertyName("port")] public int? Port { get; set; }
  [JsonPropertyName("game")] public string? Game { get; set; }

  [JsonPropertyName("gamedigGivenPortOnly")]
  public string? GamedigGivenPortOnly { get; set; }

  [JsonPropertyName("description")] public string? Description { get; set; }
  [JsonPropertyName("grpcBody")] public string? GrpcBody { get; set; }
  [JsonPropertyName("grpcEnableTls")] public string? GrpcEnableTls { get; set; }
  [JsonPropertyName("grpcMetadata")] public string? GrpcMetadata { get; set; }
  [JsonPropertyName("grpcMethod")] public string? GrpcMethod { get; set; }
  [JsonPropertyName("grpcProtobuf")] public string? GrpcProtobuf { get; set; }
  [JsonPropertyName("grpcServiceName")] public string? GrpcServiceName { get; set; }
  [JsonPropertyName("grpcUrl")] public string? GrpcUrl { get; set; }
  [JsonPropertyName("invertKeyword")] public string? InvertKeyword { get; set; }
  [JsonPropertyName("keyword")] public string? Keyword { get; set; }
  [JsonPropertyName("jsonPath")] public string? JsonPath { get; set; }
  [JsonPropertyName("expectedValue")] public string? ExpectedValue { get; set; }

  [JsonPropertyName("kafkaProducerSaslOptionsMechanism")]
  public string? KafkaProducerSaslOptionsMechanism { get; set; }

  [JsonPropertyName("kafkaProducerSsl")] public string? KafkaProducerSsl { get; set; }

  [JsonPropertyName("kafkaProducerBrokers")]
  public string? KafkaProducerBrokers { get; set; }

  [JsonPropertyName("kafkaProducerTopic")]
  public string? KafkaProducerTopic { get; set; }

  [JsonPropertyName("kafkaProducerMessage")]
  public string? KafkaProducerMessage { get; set; }

  [JsonPropertyName("databaseConnectionString")]
  public string? DatabaseConnectionString { get; set; }

  [JsonPropertyName("mqttCheckType")] public string? MqttCheckType { get; set; }
  [JsonPropertyName("mqttUsername")] public string? MqttUsername { get; set; }
  [JsonPropertyName("mqttPassword")] public string? MqttPassword { get; set; }
  [JsonPropertyName("mqttTopic")] public string? MqttTopic { get; set; }

  [JsonPropertyName("mqttSuccessMessage")]
  public string? MqttSuccessMessage { get; set; }

  [JsonPropertyName("radiusPassword")] public string? RadiusPassword { get; set; }

  [JsonPropertyName("radiusCalledStationId")]
  public string? RadiusCalledStationId { get; set; }

  [JsonPropertyName("radiusCallingStationId")]
  public string? RadiusCallingStationId { get; set; }

  [JsonPropertyName("radiusSecret")] public string? RadiusSecret { get; set; }
  [JsonPropertyName("radiusUsername")] public string? RadiusUsername { get; set; }
  [JsonPropertyName("remoteBrowser")] public string? RemoteBrowser { get; set; }

  [JsonPropertyName("remoteBrowsersToggle")]
  public string? RemoteBrowsersToggle { get; set; }

  [JsonPropertyName("pushToken")] public string? PushToken { get; set; }
}
