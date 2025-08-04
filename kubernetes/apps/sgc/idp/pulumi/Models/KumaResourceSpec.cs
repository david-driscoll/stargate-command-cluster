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

  [JsonPropertyName("accepted_statuscodes")]
  public ImmutableArray<string>? AcceptedStatusCodes { get; set; }

  [JsonPropertyName("active")] public bool? Active { get; set; }
  [JsonPropertyName("interval")] public int? Interval { get; set; }
  [JsonPropertyName("max_redirects")] public int? MaxRedirects { get; set; }
  [JsonPropertyName("max_retries")] public int? MaxRetries { get; set; }
  [JsonPropertyName("parent_name")] public string? ParentName { get; set; }
  [JsonPropertyName("proxy_id")] public string? ProxyId { get; set; }
  [JsonPropertyName("resend_interval")] public int? ResendInterval { get; set; }
  [JsonPropertyName("retry_interval")] public int? RetryInterval { get; set; }
  [JsonPropertyName("timeout")] public int? Timeout { get; set; }
  [JsonPropertyName("upside_down")] public bool? UpsideDown { get; set; }
  [JsonPropertyName("tls_ca")] public string? TlsCa { get; set; }
  [JsonPropertyName("tls_cert")] public string? TlsCert { get; set; }
  [JsonPropertyName("tls_key")] public string? TlsKey { get; set; }
  [JsonPropertyName("ignore_tls")] public bool? IgnoreTls { get; set; }

  [JsonPropertyName("expiry_notification")]
  public string? ExpiryNotification { get; set; }

  [JsonPropertyName("http_body_encoding")] public string? HttpBodyEncoding { get; set; }
  [JsonPropertyName("body")] public string? Body { get; set; }
  [JsonPropertyName("auth_domain")] public string? AuthDomain { get; set; }
  [JsonPropertyName("auth_method")] public string? AuthMethod { get; set; }
  [JsonPropertyName("auth_workstation")] public string? AuthWorkstation { get; set; }
  [JsonPropertyName("basic_auth_user")] public string? BasicAuthUser { get; set; }
  [JsonPropertyName("basic_auth_pass")] public string? BasicAuthPass { get; set; }
  [JsonPropertyName("oauth_auth_method")] public string? OauthAuthMethod { get; set; }
  [JsonPropertyName("oauth_client_id")] public string? OauthClientId { get; set; }

  [JsonPropertyName("oauth_client_secret")]
  public string? OauthClientSecret { get; set; }

  [JsonPropertyName("oauth_scopes")] public string? OauthScopes { get; set; }
  [JsonPropertyName("oauth_token_url")] public string? OauthTokenUrl { get; set; }
  [JsonPropertyName("hostname")] public string? Hostname { get; set; }
  [JsonPropertyName("packet_size")] public string? PacketSize { get; set; }
  [JsonPropertyName("docker_container")] public string? DockerContainer { get; set; }
  [JsonPropertyName("docker_host")] public string? DockerHost { get; set; }
  [JsonPropertyName("dns_resolve_server")] public string? DnsResolveServer { get; set; }
  [JsonPropertyName("dns_resolve_type")] public string? DnsResolveType { get; set; }
  [JsonPropertyName("port")] public int? Port { get; set; }
  [JsonPropertyName("game")] public string? Game { get; set; }

  [JsonPropertyName("gamedig_given_port_only")]
  public string? GamedigGivenPortOnly { get; set; }

  [JsonPropertyName("description")] public string? Description { get; set; }
  [JsonPropertyName("grpc_body")] public string? GrpcBody { get; set; }
  [JsonPropertyName("grpc_enable_tls")] public string? GrpcEnableTls { get; set; }
  [JsonPropertyName("grpc_metadata")] public string? GrpcMetadata { get; set; }
  [JsonPropertyName("grpc_method")] public string? GrpcMethod { get; set; }
  [JsonPropertyName("grpc_protobuf")] public string? GrpcProtobuf { get; set; }
  [JsonPropertyName("grpc_service_name")] public string? GrpcServiceName { get; set; }
  [JsonPropertyName("grpc_url")] public string? GrpcUrl { get; set; }
  [JsonPropertyName("invert_keyword")] public string? InvertKeyword { get; set; }
  [JsonPropertyName("keyword")] public string? Keyword { get; set; }
  [JsonPropertyName("json_path")] public string? JsonPath { get; set; }
  [JsonPropertyName("expected_value")] public string? ExpectedValue { get; set; }

  [JsonPropertyName("kafka_producer_sasl_options_mechanism")]
  public string? KafkaProducerSaslOptionsMechanism { get; set; }

  [JsonPropertyName("kafka_producer_ssl")] public string? KafkaProducerSsl { get; set; }

  [JsonPropertyName("kafka_producer_brokers")]
  public string? KafkaProducerBrokers { get; set; }

  [JsonPropertyName("kafka_producer_topic")]
  public string? KafkaProducerTopic { get; set; }

  [JsonPropertyName("kafka_producer_message")]
  public string? KafkaProducerMessage { get; set; }

  [JsonPropertyName("database_connection_string")]
  public string? DatabaseConnectionString { get; set; }

  [JsonPropertyName("mqtt_check_type")] public string? MqttCheckType { get; set; }
  [JsonPropertyName("mqtt_username")] public string? MqttUsername { get; set; }
  [JsonPropertyName("mqtt_password")] public string? MqttPassword { get; set; }
  [JsonPropertyName("mqtt_topic")] public string? MqttTopic { get; set; }

  [JsonPropertyName("mqtt_success_message")]
  public string? MqttSuccessMessage { get; set; }

  [JsonPropertyName("radius_password")] public string? RadiusPassword { get; set; }

  [JsonPropertyName("radius_called_station_id")]
  public string? RadiusCalledStationId { get; set; }

  [JsonPropertyName("radius_calling_station_id")]
  public string? RadiusCallingStationId { get; set; }

  [JsonPropertyName("radius_secret")] public string? RadiusSecret { get; set; }
  [JsonPropertyName("radius_username")] public string? RadiusUsername { get; set; }
  [JsonPropertyName("remote_browser")] public string? RemoteBrowser { get; set; }

  [JsonPropertyName("remote_browsers_toggle")]
  public string? RemoteBrowsersToggle { get; set; }

  [JsonPropertyName("push_token")] public string? PushToken { get; set; }
}
