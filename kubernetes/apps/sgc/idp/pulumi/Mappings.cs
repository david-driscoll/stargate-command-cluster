using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using k8s.Models;
using Pulumi;
using Pulumi.Uptimekuma;
using Riok.Mapperly.Abstractions;
using StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

[Mapper(AllowNullPropertyAssignment = false)]
static partial class Mappings
{
  public static ApplicationDefinitionUptime MapFromConfigMap(V1ConfigMap configMap)
  {
    return MapFromDataInternal(configMap.Data);
  }

  public static KumaResource MapMonitor(string clusterName, ApplicationDefinition definition)
  {
    Debug.Assert(definition.Spec.Uptime != null, "definition.Uptime != null");
    UptimeBase config = definition.Spec.Uptime switch
    {
      { Dns: { } dns } => dns,
      { Http: { } http } => http,
      { Ping: { } ping } => ping,
      { Docker: { } docker } => docker,
      { Gamedig: { } gamedig } => gamedig,
      { Group: { } group } => group,
      { GrpcKeyword: { } grpcKeyword } => grpcKeyword,
      { JsonQuery: { } jsonQuery } => jsonQuery,
      { KafkaProducer: { } kafkaProducer } => kafkaProducer,
      { Keyword: { } keyword } => keyword,
      { MongoDb: { } mongoDb } => mongoDb,
      { Mqtt: { } mqtt } => mqtt,
      { Mysql: { } mysql } => mysql,
      { Port: { } port } => port,
      { Postgres: { } postgres } => postgres,
      { Push: { } push } => push,
      { Radius: { } radius } => radius,
      { RealBrowser: { } realBrowser } => realBrowser,
      { Redis: { } redis } => redis,
      { Steam: { } steam } => steam,
      { SqlServer: { } sqlServer } => sqlServer,
      { TailscalePing: { } tailscalePing } => tailscalePing,
      _ => throw new ArgumentOutOfRangeException(nameof(definition.Spec.Uptime)),
    };
    return new KumaResource()
    {
      ApiVersion = "autokuma.bigboot.dev/v1",
      Kind = "KumaEntity",
      Metadata = new V1ObjectMeta()
      {
        Name = ResourceName(clusterName, definition),
        Labels = new Dictionary<string, string>
        {
          ["driscoll.dev/cluster"] = clusterName,
        }
      },
      Spec = new { Config = config }
    };
  }

  public static ApplicationDefinitionUptime MapFromSecret(V1Secret secret)
  {
    return MapFromDataInternal(secret.Data.ToDictionary(z => z.Key, z => Encoding.UTF8.GetString(z.Value)));
  }
  public static T MapFromResourceArgs<T>(T args, IDictionary<string, string> data) where T : ResourceArgs
  {
    var properties = args.GetType().GetProperties().ToDictionary(z => z.Name, z => z, StringComparer.OrdinalIgnoreCase);
    foreach (var kvp in data)
    {
      if (properties.TryGetValue(kvp.Key, out var property))
      {
        var value = JsonSerializer.Deserialize(kvp.Value, property.PropertyType);
        property.SetValue(args, value);
      }
      else
      {
        throw new ArgumentException($"Property '{kvp.Key}' not found in type '{typeof(T).Name}'.");
      }
    }

    return args;
  }
  private static ApplicationDefinitionUptime MapFromDataInternal(IDictionary<string, string> data)
  {
    var jsonData = JsonSerializer.Serialize(data);
    return data["type"] switch
    {
      "http" => new ApplicationDefinitionUptime() { Http = JsonSerializer.Deserialize<HttpUptime>(jsonData), },
      "ping" => new ApplicationDefinitionUptime() { Ping = JsonSerializer.Deserialize<PingUptime>(jsonData), },
      "docker" => new ApplicationDefinitionUptime()
        { Docker = JsonSerializer.Deserialize<DockerUptime>(jsonData), },
      "dns" => new ApplicationDefinitionUptime() { Dns = JsonSerializer.Deserialize<DnsUptime>(jsonData), },
      "gamedig" => new ApplicationDefinitionUptime()
        { Gamedig = JsonSerializer.Deserialize<GamedigUptime>(jsonData), },
      "group" => new ApplicationDefinitionUptime() { Group = JsonSerializer.Deserialize<GroupUptime>(jsonData), },
      "grpc-keyword" => new ApplicationDefinitionUptime()
        { GrpcKeyword = JsonSerializer.Deserialize<GrpcKeywordUptime>(jsonData), },
      "json-query" => new ApplicationDefinitionUptime()
        { JsonQuery = JsonSerializer.Deserialize<JsonQueryUptime>(jsonData), },
      "kafka-producer" => new ApplicationDefinitionUptime()
        { KafkaProducer = JsonSerializer.Deserialize<KafkaProducerUptime>(jsonData), },
      "keyword" => new ApplicationDefinitionUptime()
        { Keyword = JsonSerializer.Deserialize<KeywordUptime>(jsonData), },
      "mongodb" => new ApplicationDefinitionUptime()
        { MongoDb = JsonSerializer.Deserialize<MongoDbUptime>(jsonData), },
      "mqtt" => new ApplicationDefinitionUptime() { Mqtt = JsonSerializer.Deserialize<MqttUptime>(jsonData), },
      "mysql" => new ApplicationDefinitionUptime() { Mysql = JsonSerializer.Deserialize<MysqlUptime>(jsonData), },
      "port" => new ApplicationDefinitionUptime() { Port = JsonSerializer.Deserialize<PortUptime>(jsonData), },
      "postgres" => new ApplicationDefinitionUptime()
        { Postgres = JsonSerializer.Deserialize<PostgresUptime>(jsonData), },
      "push" => new ApplicationDefinitionUptime() { Push = JsonSerializer.Deserialize<PushUptime>(jsonData), },
      "radius" => new ApplicationDefinitionUptime()
        { Radius = JsonSerializer.Deserialize<RadiusUptime>(jsonData), },
      "real-browser" => new ApplicationDefinitionUptime()
        { RealBrowser = JsonSerializer.Deserialize<RealBrowserUptime>(jsonData), },
      "redis" => new ApplicationDefinitionUptime() { Redis = JsonSerializer.Deserialize<RedisUptime>(jsonData), },
      "steam" => new ApplicationDefinitionUptime() { Steam = JsonSerializer.Deserialize<SteamUptime>(jsonData), },
      "sqlserver" => new ApplicationDefinitionUptime()
        { SqlServer = JsonSerializer.Deserialize<SqlServerUptime>(jsonData), },
      "tailscale-ping" => new ApplicationDefinitionUptime()
        { TailscalePing = JsonSerializer.Deserialize<TailscalePingUptime>(jsonData), },
      _ => throw new ArgumentOutOfRangeException()
    };
  }


  public static string ResourceName(string clusterName, ApplicationDefinition resource) =>
    $"{Prefix(clusterName, resource)}-{resource.Metadata.Name}";

  private static string Prefix(string clusterName, ApplicationDefinition resource) =>
    resource.Namespace() is { } ns && ns == clusterName ? clusterName : $"{clusterName}-{resource.Namespace()}";
}
