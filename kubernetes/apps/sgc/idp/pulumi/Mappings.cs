using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using k8s.Models;
using Pulumi;
using Pulumi.Authentik;
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
    var config = definition.Spec.Uptime switch
    {
      { Dns: { } dns } => MapToUptime(dns),
      { Http: { } http } => MapToUptime(http),
      { Ping: { } ping } => MapToUptime(ping),
      { Docker: { } docker } => MapToUptime(docker),
      { Gamedig: { } gamedig } => MapToUptime(gamedig),
      { Group: { } group } => MapToUptime(group),
      { GrpcKeyword: { } grpcKeyword } => MapToUptime(grpcKeyword),
      { JsonQuery: { } jsonQuery } => MapToUptime(jsonQuery),
      { KafkaProducer: { } kafkaProducer } => MapToUptime(kafkaProducer),
      { Keyword: { } keyword } => MapToUptime(keyword),
      { MongoDb: { } mongoDb } => MapToUptime(mongoDb),
      { Mqtt: { } mqtt } => MapToUptime(mqtt),
      { Mysql: { } mysql } => MapToUptime(mysql),
      { Port: { } port } => MapToUptime(port),
      { Postgres: { } postgres } => MapToUptime(postgres),
      { Push: { } push } => MapToUptime(push),
      { Radius: { } radius } => MapToUptime(radius),
      { RealBrowser: { } realBrowser } => MapToUptime(realBrowser),
      { Redis: { } redis } => MapToUptime(redis),
      { Steam: { } steam } => MapToUptime(steam),
      { SqlServer: { } sqlServer } => MapToUptime(sqlServer),
      { TailscalePing: { } tailscalePing } => MapToUptime(tailscalePing),
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
      Spec = new () { Config = config, },
    };
  }

  public static partial KumaResourceConfigSpec MapToUptime(DnsUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(HttpUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(PingUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(DockerUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(GamedigUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(GroupUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(GrpcKeywordUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(JsonQueryUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(KafkaProducerUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(KeywordUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(MongoDbUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(MqttUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(MysqlUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(PortUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(PostgresUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(PushUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(RadiusUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(RealBrowserUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(RedisUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(SteamUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(SqlServerUptime uptime);
  public static partial KumaResourceConfigSpec MapToUptime(TailscalePingUptime uptime);

  public static partial AuthentikProviderSaml MapToSaml(AuthentikSpec spec);
  public static partial AuthentikProviderOauth2 MapToOauth2(AuthentikSpec spec);
  public static partial AuthentikProviderScim MapToScim(AuthentikSpec spec);
  public static partial AuthentikProviderSsf MapToSsf(AuthentikSpec spec);
  public static partial AuthentikProviderProxy MapToProxy(AuthentikSpec spec);
  public static partial AuthentikProviderRadius MapToRadius(AuthentikSpec spec);
  public static partial AuthentikProviderRac MapToRac(AuthentikSpec spec);
  public static partial AuthentikProviderLdap MapToLdap(AuthentikSpec spec);
  public static partial AuthentikProviderMicrosoftEntra MapToMicrosoftEntra(AuthentikSpec spec);
  public static partial AuthentikProviderGoogleWorkspace MapToGoogleWorkspace(AuthentikSpec spec);
  public static partial ProviderProxyArgs CreateProvider(AuthentikProviderProxy instance);
  public static partial ProviderOauth2Args CreateProvider(AuthentikProviderOauth2 instance);

  public static partial ProviderLdapArgs CreateProvider(AuthentikProviderLdap instance);
  public static partial ProviderSamlArgs CreateProvider(AuthentikProviderSaml instance);
  public static partial ProviderRacArgs CreateProvider(AuthentikProviderRac instance);
  public static partial ProviderRadiusArgs CreateProvider(AuthentikProviderRadius instance);
  public static partial ProviderSsfArgs CreateProvider(AuthentikProviderSsf instance);
  public static partial ProviderScimArgs CreateProvider(AuthentikProviderScim instance);
  public static partial ProviderMicrosoftEntraArgs CreateProvider(AuthentikProviderMicrosoftEntra instance);
  public static partial ProviderGoogleWorkspaceArgs CreateProvider(AuthentikProviderGoogleWorkspace instance);

  public static ApplicationDefinitionUptime MapFromSecret(V1Secret secret)
  {
    return MapFromDataInternal(secret.Data.ToDictionary(z => z.Key, z => Encoding.UTF8.GetString(z.Value)));
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
