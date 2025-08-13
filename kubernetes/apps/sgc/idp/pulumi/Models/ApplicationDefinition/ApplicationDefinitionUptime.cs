using System.Text.Json.Serialization;
using applications.Models.UptimeKuma;
using YamlDotNet.Serialization;

namespace applications.Models.ApplicationDefinition;

public record ApplicationDefinitionUptime
{
  [YamlMember(Alias = "http")]
  [JsonPropertyName("http")]
  public HttpUptime? Http { get; set; }
  [YamlMember(Alias = "ping")]
  [JsonPropertyName("ping")]
  public PingUptime? Ping { get; set; }
  [YamlMember(Alias = "docker")]
  [JsonPropertyName("docker")]
  public DockerUptime? Docker { get; set; }
  [YamlMember(Alias = "dns")]
  [JsonPropertyName("dns")]
  public DnsUptime? Dns { get; set; }
  [YamlMember(Alias = "gamedig")]
  [JsonPropertyName("gamedig")]
  public GamedigUptime? Gamedig { get; set; }
  [YamlMember(Alias = "group")]
  [JsonPropertyName("group")]
  public GroupUptime? Group { get; set; }
  [YamlMember(Alias = "grpc-keyword")]
  [JsonPropertyName("grpc-keyword")]
  public GrpcKeywordUptime? GrpcKeyword { get; set; }
  [YamlMember(Alias = "json-query")]
  [JsonPropertyName("json-query")]
  public JsonQueryUptime? JsonQuery { get; set; }
  [YamlMember(Alias = "kafka-producer")]
  [JsonPropertyName("kafka-producer")]
  public KafkaProducerUptime? KafkaProducer { get; set; }
  [YamlMember(Alias = "keyword")]
  [JsonPropertyName("keyword")]
  public KeywordUptime? Keyword { get; set; }
  [YamlMember(Alias = "mongodb")]
  [JsonPropertyName("mongodb")]
  public MongoDbUptime? MongoDb { get; set; }
  [YamlMember(Alias = "mqtt")]
  [JsonPropertyName("mqtt")]
  public MqttUptime? Mqtt { get; set; }
  [YamlMember(Alias = "mysql")]
  [JsonPropertyName("mysql")]
  public MysqlUptime? Mysql { get; set; }
  [YamlMember(Alias = "port")]
  [JsonPropertyName("port")]
  public PortUptime? Port { get; init; }
  [YamlMember(Alias = "postgres")]
  [JsonPropertyName("postgres")]
  public PostgresUptime? Postgres { get; init; }
  [YamlMember(Alias = "push")]
  [JsonPropertyName("push")]
  public PushUptime? Push { get; init; }
  [YamlMember(Alias = "radius")]
  [JsonPropertyName("radius")]
  public RadiusUptime? Radius { get; init; }
  [YamlMember(Alias = "real-browser")]
  [JsonPropertyName("real-browser")]
  public RealBrowserUptime? RealBrowser { get; init; }
  [YamlMember(Alias = "redis")]
  [JsonPropertyName("redis")]
  public RedisUptime? Redis { get; init; }
  [YamlMember(Alias = "steam")]
  [JsonPropertyName("steam")]
  public SteamUptime? Steam { get; init; }
  [YamlMember(Alias = "sqlserver")]
  [JsonPropertyName("sqlserver")]
  public SqlServerUptime? SqlServer { get; init; }
  [YamlMember(Alias = "tailscale-ping")]
  [JsonPropertyName("tailscale-ping")]
  public TailscalePingUptime? TailscalePing { get; init; }
}
