using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi
{

  public class ApplicationDefinitionList : KubernetesObject, IMetadata<V1ListMeta>
  {
    public V1ListMeta Metadata { get; set; }
    public List<ApplicationDefinition> Items { get; set; }
  }

  public class ApplicationDefinition : KubernetesObject, IMetadata<V1ObjectMeta>
  {
    [JsonPropertyName("metadata")]
    public V1ObjectMeta Metadata { get; set; }

    [JsonPropertyName("spec")]
    public ApplicationDefinitionSpec Spec { get; set; }

    [JsonPropertyName("status")]
    public ApplicationDefinitionStatus Status { get; set; }
  }

  public record ApplicationDefinitionStatus { }

  public record ApplicationDefinitionSpec
  {
    [JsonPropertyName("name")]
    public string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("category")]
    public string Category { get; init; }

    [JsonPropertyName("uptime")]
    public ApplicationDefinitionUptime? Uptime { get; init; }

    [JsonPropertyName("uptimeFrom")]
    public UptimeFrom? UptimeFrom { get; init; }

    [JsonPropertyName("authentik")]
    public Authentik? Authentik { get; init; }

    [JsonPropertyName("authentikFrom")]
    public AuthentikFrom? AuthentikFrom { get; init; }
  }

  public record ApplicationDefinitionUptime
  {
    [JsonPropertyName("http")]
    public HttpUptime? Http { get; init; }
    [JsonPropertyName("ping")]
    public PingUptime? Ping { get; init; }
    [JsonPropertyName("docker")]
    public DockerUptime? Docker { get; init; }
    [JsonPropertyName("dns")]
    public DnsUptime? Dns { get; init; }
    [JsonPropertyName("gamedig")]
    public GamedigUptime? Gamedig { get; init; }
    [JsonPropertyName("group")]
    public GroupUptime? Group { get; init; }
    [JsonPropertyName("grpc-keyword")]
    public GrpcKeywordUptime? GrpcKeyword { get; init; }
    [JsonPropertyName("json-query")]
    public JsonQueryUptime? JsonQuery { get; init; }
    [JsonPropertyName("kafka-producer")]
    public KafkaProducerUptime? KafkaProducer { get; init; }
    [JsonPropertyName("keyword")]
    public KeywordUptime? Keyword { get; init; }
    [JsonPropertyName("mongodb")]
    public MongoDbUptime? MongoDb { get; init; }
    [JsonPropertyName("mqtt")]
    public MqttUptime? Mqtt { get; init; }
    [JsonPropertyName("mysql")]
    public MysqlUptime? Mysql { get; init; }
    [JsonPropertyName("port")]
    public PortUptime? Port { get; init; }
    [JsonPropertyName("postgres")]
    public PostgresUptime? Postgres { get; init; }
    [JsonPropertyName("push")]
    public PushUptime? Push { get; init; }
    [JsonPropertyName("radius")]
    public RadiusUptime? Radius { get; init; }
    [JsonPropertyName("real-browser")]
    public RealBrowserUptime? RealBrowser { get; init; }
    [JsonPropertyName("redis")]
    public RedisUptime? Redis { get; init; }
    [JsonPropertyName("steam")]
    public SteamUptime? Steam { get; init; }
    [JsonPropertyName("sqlserver")]
    public SqlServerUptime? SqlServer { get; init; }
    [JsonPropertyName("tailscale-ping")]
    public TailscalePingUptime? TailscalePing { get; init; }
  }

  public abstract record UptimeBase
  {
    public abstract string Type { get; }
   public bool Active { get; init; } = true;
    public int? Interval { get; init; } = 5 * 60;
    public int? MaxRetries { get; init; } = 3;
    public string? ParentName { get; init; }
    public int? RetryInterval { get; init; } = 60;
    public bool UpsideDown { get; init; }
  }

  // Example for one uptime type, others follow similar pattern
  public record HttpUptime : UptimeBase
  {
    public override string Type { get; } = "http";
    public string? Url { get; init; }
    public string? Method { get; init; }
    public int? Timeout { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
    public int? MaxRedirects { get; init; }
    public string? ProxyId { get; init; }
    public int? ResendInterval { get; init; }
    public string? TlsCa { get; init; }
    public string? TlsCert { get; init; }
    public string? TlsKey { get; init; }
    public bool? IgnoreTls { get; init; }
    public bool? ExpiryNotification { get; init; }
    public string? HttpBodyEncoding { get; init; }
    public string? Body { get; init; }
    public string? AuthDomain { get; init; }
    public string? AuthMethod { get; init; }
    public string? AuthWorkstation { get; init; }
    public string? BasicAuthUser { get; init; }
    public string? BasicAuthPass { get; init; }
    public string? OauthAuthMethod { get; init; }
    public string? OauthClientId { get; init; }
    public string? OauthClientSecret { get; init; }
    public string? OauthScopes { get; init; }
    public string? OauthTokenUrl { get; init; }
  }

  public record PingUptime : UptimeBase
  {
    public override string Type { get; } = "ping";
    public string Hostname { get; init; }
    public int? PacketSize { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
  }

  public record DockerUptime : UptimeBase
  {
    public override string Type { get; } = "docker";
    public string DockerContainer { get; init; }
    public string DockerHost { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
  }

  public record DnsUptime : UptimeBase
  {

    public override string Type { get; } = "dns";
    public string Hostname { get; init; }
    public string DnsResolveServer { get; init; }
    public string DnsResolveType { get; init; }
    public int? Port { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

  }

  public record GamedigUptime : UptimeBase
  {
    public override string Type { get; } = "gamedig";
    public string Game { get; init; }
    public bool? GamedigGivenPortOnly { get; init; }
    public string Hostname { get; init; }
    public int? Port { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;


  }

  public record GroupUptime : UptimeBase
  {
    public override string Type { get; } = "group";
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
  }

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

  public record JsonQueryUptime : UptimeBase
  {
    public override string Type { get; } = "json-query";
    public string Url { get; init; }
    public string JsonPath { get; init; }
    public string ExpectedValue { get; init; }
    public string Method { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
    public string? AuthDomain { get; init; }
    public string? AuthMethod { get; init; }
    public string? AuthWorkstation { get; init; }
    public string? BasicAuthUser { get; init; }
    public string? BasicAuthPass { get; init; }
    public string? Body { get; init; }

    public bool? ExpiryNotification { get; init; }
    public string? HttpBodyEncoding { get; init; }
    public bool? IgnoreTls { get; init; }
    public int? MaxRedirects { get; init; }
    public string? OauthAuthMethod { get; init; }
    public string? OauthClientId { get; init; }
    public string? OauthClientSecret { get; init; }
    public string? OauthScopes { get; init; }
    public string? OauthTokenUrl { get; init; }
    public string? ProxyId { get; init; }
    public int? ResendInterval { get; init; }
    public int? Timeout { get; init; }
    public string? TlsCa { get; init; }
    public string? TlsCert { get; init; }
    public string? TlsKey { get; init; }
  }

  public record KafkaProducerUptime : UptimeBase
  {
    public override string Type { get; } = "kafka-producer";
    public string? KafkaProducerSaslOptionsMechanism { get; init; }
    public bool? KafkaProducerSsl { get; init; }
    public string KafkaProducerBrokers { get; init; }
    public string KafkaProducerTopic { get; init; }
    public string KafkaProducerMessage { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

  }

  public record KeywordUptime : UptimeBase
  {
    public override string Type { get; } = "keyword";
    public string Url { get; init; }
    public string Keyword { get; init; }
    public bool? InvertKeyword { get; init; }
    public string Method { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
    public string? AuthDomain { get; init; }
    public string? AuthMethod { get; init; }
    public string? AuthWorkstation { get; init; }
    public string? BasicAuthUser { get; init; }
    public string? BasicAuthPass { get; init; }
    public string? Body { get; init; }

    public bool? ExpiryNotification { get; init; }
    public string? HttpBodyEncoding { get; init; }
    public bool? IgnoreTls { get; init; }
    public int? MaxRedirects { get; init; }
    public string? OauthAuthMethod { get; init; }
    public string? OauthClientId { get; init; }
    public string? OauthClientSecret { get; init; }
    public string? OauthScopes { get; init; }
    public string? OauthTokenUrl { get; init; }
    public string? ProxyId { get; init; }
    public int? ResendInterval { get; init; }
    public int? Timeout { get; init; }
    public string? TlsCa { get; init; }
    public string? TlsCert { get; init; }
    public string? TlsKey { get; init; }
  }

  public record MongoDbUptime : UptimeBase
  {
    public override string Type { get; } = "mongodb";
    public string DatabaseConnectionString { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

  }

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

  public record MysqlUptime : UptimeBase
  {
    public override string Type { get; } = "mqtt";
    public string DatabaseConnectionString { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
  }

  public record PortUptime : UptimeBase
  {
    public override string Type { get; } = "port";
    public string Hostname { get; init; }
    public int? Port { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

  }

  public record PostgresUptime : UptimeBase
  {
    public override string Type { get; } = "postgres";
    public string DatabaseConnectionString { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
  }

  public record PushUptime : UptimeBase
  {
    public override string Type { get; } = "push";
    public string PushToken { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
  }

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

  public record RealBrowserUptime : UptimeBase
  {
    public override string Type { get; } = "real-browser";
    public string RemoteBrowser { get; init; }
    public bool? RemoteBrowsersToggle { get; init; }
    public string Url { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
  }

  public record RedisUptime : UptimeBase
  {
    public override string Type { get; } = "redis";
    public string DatabaseConnectionString { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
  }

  public record SteamUptime : UptimeBase
  {
    public override string Type { get; } = "steam";
    public string Hostname { get; init; }
    public int? Port { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

  }

  public record SqlServerUptime : UptimeBase
  {
    public override string Type { get; } = "sqlserver";
    public string DatabaseConnectionString { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

  }

  public record TailscalePingUptime : UptimeBase
  {
    public override string Type { get; } = "tailscale-ping";
    public string Hostname { get; init; }
    public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

  }

  public record UptimeFrom
  {
    [JsonPropertyName("type")]
    public string Type { get; init; }
    [JsonPropertyName("name")]
    public string Name { get; init; }
  }

  public record Authentik
  {
    [JsonPropertyName("provider")]
    public string Provider { get; init; }
    [JsonPropertyName("slug")]
    public string? Slug { get; init; }
    [JsonPropertyName("url")]
    public string? Url { get; init; }
    [JsonPropertyName("icon")]
    public string? Icon { get; init; }
    [JsonPropertyName("config")]
    public Dictionary<string, string>? Config { get; init; }
  }

  public record AuthentikFrom
  {
    [JsonPropertyName("type")]
    public string Type { get; init; }
    [JsonPropertyName("name")]
    public string Name { get; init; }
  }
}
