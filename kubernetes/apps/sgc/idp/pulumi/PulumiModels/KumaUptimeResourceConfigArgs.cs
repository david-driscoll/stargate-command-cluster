using System;
using Pulumi;

namespace applications.PulumiModels;

class KumaUptimeResourceConfigArgs : ResourceArgs
{
  [Input("type")] public Input<string>? Type { get; set; }
  [Input("name")] public required Input<string> Name { get; set; }
  [Input("url")] public Input<string>? Url { get; set; }

  [Input("method")] public Input<string>? Method { get; set; }

  [Input("accepted_statuscodes")] public InputList<string>? AcceptedStatusCodes { get; set; }
  [Input("active")] public Input<bool>? Active { get; set; } = true;
  [Input("interval")] public Input<int>? Interval { get; set; } = 300;
  [Input("max_redirects")] public Input<int>? MaxRedirects { get; set; }
  [Input("max_retries")] public Input<int>? MaxRetries { get; set; } = 3;
  [Input("parent_name")] public Input<string>? ParentName { get; set; }
  [Input("proxy_id")] public Input<string>? ProxyId { get; set; }
  [Input("resend_interval")] public Input<int>? ResendInterval { get; set; } = 300;
  [Input("retry_interval")] public Input<int>? RetryInterval { get; set; } = 60;
  [Input("timeout")] public Input<int>? Timeout { get; set; } = 60;
  [Input("upside_down")] public Input<bool>? UpsideDown { get; set; }
  [Input("tls_ca")] public Input<string>? TlsCa { get; set; }
  [Input("tls_cert")] public Input<string>? TlsCert { get; set; }
  [Input("tls_key")] public Input<string>? TlsKey { get; set; }
  [Input("ignore_tls")] public Input<bool>? IgnoreTls { get; set; }

  [Input("expiry_notification")] public Input<bool>? ExpiryNotification { get; set; }

  [Input("http_body_encoding")] public Input<string>? HttpBodyEncoding { get; set; }
  [Input("body")] public Input<string>? Body { get; set; }
  [Input("auth_domain")] public Input<string>? AuthDomain { get; set; }
  [Input("auth_method")] public Input<string>? AuthMethod { get; set; }
  [Input("auth_workstation")] public Input<string>? AuthWorkstation { get; set; }
  [Input("basic_auth_user")] public Input<string>? BasicAuthUser { get; set; }
  [Input("basic_auth_pass")] public Input<string>? BasicAuthPass { get; set; }
  [Input("oauth_auth_method")] public Input<string>? OauthAuthMethod { get; set; }
  [Input("oauth_client_id")] public Input<string>? OauthClientId { get; set; }

  [Input("oauth_client_secret")] public Input<string>? OauthClientSecret { get; set; }

  [Input("oauth_scopes")] public Input<string>? OauthScopes { get; set; }
  [Input("oauth_token_url")] public Input<string>? OauthTokenUrl { get; set; }
  [Input("hostname")] public Input<string>? Hostname { get; set; }
  [Input("packet_size")] public Input<int>? PacketSize { get; set; }
  [Input("docker_container")] public Input<string>? DockerContainer { get; set; }
  [Input("docker_host")] public Input<string>? DockerHost { get; set; }
  [Input("dns_resolve_server")] public Input<string>? DnsResolveServer { get; set; }
  [Input("dns_resolve_type")] public Input<string>? DnsResolveType { get; set; }
  [Input("port")] public Input<int>? Port { get; set; }
  [Input("game")] public Input<string>? Game { get; set; }

  [Input("gamedig_given_port_only")] public Input<bool>? GamedigGivenPortOnly { get; set; }

  [Input("description")] public Input<string>? Description { get; set; }
  [Input("grpc_body")] public Input<string>? GrpcBody { get; set; }
  [Input("grpc_enable_tls")] public Input<bool>? GrpcEnableTls { get; set; }
  [Input("grpc_metadata")] public Input<string>? GrpcMetadata { get; set; }
  [Input("grpc_method")] public Input<string>? GrpcMethod { get; set; }
  [Input("grpc_protobuf")] public Input<string>? GrpcProtobuf { get; set; }
  [Input("grpc_service_name")] public Input<string>? GrpcServiceName { get; set; }
  [Input("grpc_url")] public Input<string>? GrpcUrl { get; set; }
  [Input("invert_keyword")] public Input<bool>? InvertKeyword { get; set; }
  [Input("keyword")] public Input<string>? Keyword { get; set; }
  [Input("json_path")] public Input<string>? JsonPath { get; set; }
  [Input("expected_value")] public Input<string>? ExpectedValue { get; set; }

  [Input("kafka_producer_sasl_options_mechanism")]
  public Input<string>? KafkaProducerSaslOptionsMechanism { get; set; }

  [Input("kafka_producer_ssl")] public Input<bool>? KafkaProducerSsl { get; set; }

  [Input("kafka_producer_brokers")] public Input<string>? KafkaProducerBrokers { get; set; }

  [Input("kafka_producer_topic")] public Input<string>? KafkaProducerTopic { get; set; }

  [Input("kafka_producer_message")] public Input<string>? KafkaProducerMessage { get; set; }

  [Input("database_connection_string")] public Input<string>? DatabaseConnectionString { get; set; }

  [Input("mqtt_check_type")] public Input<string>? MqttCheckType { get; set; }
  [Input("mqtt_username")] public Input<string>? MqttUsername { get; set; }
  [Input("mqtt_password")] public Input<string>? MqttPassword { get; set; }
  [Input("mqtt_topic")] public Input<string>? MqttTopic { get; set; }

  [Input("mqtt_success_message")] public Input<string>? MqttSuccessMessage { get; set; }

  [Input("radius_password")] public Input<string>? RadiusPassword { get; set; }

  [Input("radius_called_station_id")] public Input<string>? RadiusCalledStationId { get; set; }

  [Input("radius_calling_station_id")] public Input<string>? RadiusCallingStationId { get; set; }

  [Input("radius_secret")] public Input<string>? RadiusSecret { get; set; }
  [Input("radius_username")] public Input<string>? RadiusUsername { get; set; }
  [Input("remote_browser")] public Input<string>? RemoteBrowser { get; set; }

  [Input("remote_browsers_toggle")] public Input<bool>? RemoteBrowsersToggle { get; set; }

  [Input("push_token")] public Input<string>? PushToken { get; set; }

  protected void ValidateMember(Type memberType, string fullName) { }
}
