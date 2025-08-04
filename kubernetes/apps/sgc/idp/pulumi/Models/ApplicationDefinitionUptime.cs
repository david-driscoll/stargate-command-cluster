using System.Text.Json.Serialization;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

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
