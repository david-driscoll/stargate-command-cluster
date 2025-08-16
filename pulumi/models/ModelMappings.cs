using System;
using System.Collections.Generic;
using applications.Models.Authentik;
using applications.Models.UptimeKuma;
using k8s;
using k8s.Models;
using models.Applications;
using Riok.Mapperly.Abstractions;

namespace models;

[Mapper(AllowNullPropertyAssignment = false)]
public static partial class ModelMappings
{
  public static (string ClusterName, string ClusterTitle, string Namespace) GetClusterNameAndTitle<T>(
    this IMetadata<T> definition)
    where T : V1ObjectMeta
  {
    var clusterName = definition.Metadata.Labels["driscoll.dev/cluster"];
    var @namespace = definition.Metadata.Labels["driscoll.dev/namespace"];
    var clusterTitle =definition.Metadata.Annotations["driscoll.dev/clusterTitle"];

    return (clusterName, clusterTitle, @namespace);
  }

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

  public static ApplicationDefinitionUptime MapFromUptimeData(IDictionary<string, string> data)
  {
    var jsonData = KubernetesJson.Serialize(data);
    return data["type"] switch
    {
      "http" => new ApplicationDefinitionUptime() { Http = KubernetesJson.Deserialize<HttpUptime>(jsonData), },
      "ping" => new ApplicationDefinitionUptime() { Ping = KubernetesJson.Deserialize<PingUptime>(jsonData), },
      "docker" => new ApplicationDefinitionUptime()
        { Docker = KubernetesJson.Deserialize<DockerUptime>(jsonData), },
      "dns" => new ApplicationDefinitionUptime() { Dns = KubernetesJson.Deserialize<DnsUptime>(jsonData), },
      "gamedig" => new ApplicationDefinitionUptime()
        { Gamedig = KubernetesJson.Deserialize<GamedigUptime>(jsonData), },
      "group" => new ApplicationDefinitionUptime() { Group = KubernetesJson.Deserialize<GroupUptime>(jsonData), },
      "grpc-keyword" => new ApplicationDefinitionUptime()
        { GrpcKeyword = KubernetesJson.Deserialize<GrpcKeywordUptime>(jsonData), },
      "json-query" => new ApplicationDefinitionUptime()
        { JsonQuery = KubernetesJson.Deserialize<JsonQueryUptime>(jsonData), },
      "kafka-producer" => new ApplicationDefinitionUptime()
        { KafkaProducer = KubernetesJson.Deserialize<KafkaProducerUptime>(jsonData), },
      "keyword" => new ApplicationDefinitionUptime()
        { Keyword = KubernetesJson.Deserialize<KeywordUptime>(jsonData), },
      "mongodb" => new ApplicationDefinitionUptime()
        { MongoDb = KubernetesJson.Deserialize<MongoDbUptime>(jsonData), },
      "mqtt" => new ApplicationDefinitionUptime() { Mqtt = KubernetesJson.Deserialize<MqttUptime>(jsonData), },
      "mysql" => new ApplicationDefinitionUptime() { Mysql = KubernetesJson.Deserialize<MysqlUptime>(jsonData), },
      "port" => new ApplicationDefinitionUptime() { Port = KubernetesJson.Deserialize<PortUptime>(jsonData), },
      "postgres" => new ApplicationDefinitionUptime()
        { Postgres = KubernetesJson.Deserialize<PostgresUptime>(jsonData), },
      "push" => new ApplicationDefinitionUptime() { Push = KubernetesJson.Deserialize<PushUptime>(jsonData), },
      "radius" => new ApplicationDefinitionUptime()
        { Radius = KubernetesJson.Deserialize<RadiusUptime>(jsonData), },
      "real-browser" => new ApplicationDefinitionUptime()
        { RealBrowser = KubernetesJson.Deserialize<RealBrowserUptime>(jsonData), },
      "redis" => new ApplicationDefinitionUptime() { Redis = KubernetesJson.Deserialize<RedisUptime>(jsonData), },
      "steam" => new ApplicationDefinitionUptime() { Steam = KubernetesJson.Deserialize<SteamUptime>(jsonData), },
      "sqlserver" => new ApplicationDefinitionUptime()
        { SqlServer = KubernetesJson.Deserialize<SqlServerUptime>(jsonData), },
      "tailscale-ping" => new ApplicationDefinitionUptime()
        { TailscalePing = KubernetesJson.Deserialize<TailscalePingUptime>(jsonData), },
      _ => throw new ArgumentOutOfRangeException()
    };
  }
}
