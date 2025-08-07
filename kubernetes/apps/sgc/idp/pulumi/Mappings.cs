using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using authentik.Models;
using k8s;
using k8s.Models;
using Pulumi;
using Pulumi.Authentik;
using Pulumi.Uptimekuma;
using Riok.Mapperly.Abstractions;

[Mapper(AllowNullPropertyAssignment = false)]
static partial class Mappings
{
  public static KumaUptimeResourceConfigArgs MapMonitor(string clusterName, ApplicationDefinition definition)
  {
    Debug.Assert(definition.Spec.Uptime != null, "definition.Uptime != null");
    var args = new KumaUptimeResourceConfigArgs()
    {
      Name = definition.Spec.Name,
      Active = true,
    };
    switch (definition.Spec.Uptime)
    {
      case { Dns: { } dns }:
        MapToUptime(args, dns);
        break;
      case { Http: { } http }:
        MapToUptime(args, http);
        break;
      case { Ping: { } ping }:
        MapToUptime(args, ping);
        break;
      case { Docker: { } docker }:
        MapToUptime(args, docker);
        break;
      case { Gamedig: { } gamedig }:
        MapToUptime(args, gamedig);
        break;
      case { Group: { } group }:
        MapToUptime(args, group);
        break;
      case { GrpcKeyword: { } grpcKeyword }:
        MapToUptime(args, grpcKeyword);
        break;
      case { JsonQuery: { } jsonQuery }:
        MapToUptime(args, jsonQuery);
        break;
      case { KafkaProducer: { } kafkaProducer }:
        MapToUptime(args, kafkaProducer);
        break;
      case { Keyword: { } keyword }:
        MapToUptime(args, keyword);
        break;
      case { MongoDb: { } mongoDb }:
        MapToUptime(args, mongoDb);
        break;
      case { Mqtt: { } mqtt }:
        MapToUptime(args, mqtt);
        break;
      case { Mysql: { } mysql }:
        MapToUptime(args, mysql);
        break;
      case { Port: { } port }:
        MapToUptime(args, port);
        break;
      case { Postgres: { } postgres }:
        MapToUptime(args, postgres);
        break;
      case { Push: { } push }:
        MapToUptime(args, push);
        break;
      case { Radius: { } radius }:
        MapToUptime(args, radius);
        break;
      case { RealBrowser: { } realBrowser }:
        MapToUptime(args, realBrowser);
        break;
      case { Redis: { } redis }:
        MapToUptime(args, redis);
        break;
      case { Steam: { } steam }:
        MapToUptime(args, steam);
        break;
      case { SqlServer: { } sqlServer }:
        MapToUptime(args, sqlServer);
        break;
      case { TailscalePing: { } tailscalePing }:
        MapToUptime(args, tailscalePing);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(definition.Spec.Uptime));
    }

    return args;
  }

  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, DnsUptime uptime);

  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, HttpUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PingUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, DockerUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, GamedigUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, GroupUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, GrpcKeywordUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, JsonQueryUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, KafkaProducerUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, KeywordUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, MongoDbUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, MqttUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, MysqlUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PortUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PostgresUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PushUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, RadiusUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, RealBrowserUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, RedisUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, SteamUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, SqlServerUptime uptime);
  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, TailscalePingUptime uptime);

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

  public static partial ProviderProxyArgs CreateProvider(AuthentikProviderProxy instance,
    Input<string> authorizationFlow, Input<string> invalidationFlow, Input<string>? authenticationFlow);

  public static partial ProviderOauth2Args CreateProvider(AuthentikProviderOauth2 instance,
    Input<string> authorizationFlow, Input<string> invalidationFlow, Input<string>? authenticationFlow);

  public static partial ProviderLdapArgs CreateProvider(AuthentikProviderLdap instance, Input<string> authorizationFlow,
    Input<string> invalidationFlow, Input<string>? authenticationFlow);

  public static partial ProviderSamlArgs CreateProvider(AuthentikProviderSaml instance, Input<string> authorizationFlow,
    Input<string> invalidationFlow, Input<string>? authenticationFlow);

  public static partial ProviderRacArgs CreateProvider(AuthentikProviderRac instance, Input<string> authorizationFlow,
    Input<string> invalidationFlow, Input<string>? authenticationFlow);

  public static partial ProviderRadiusArgs CreateProvider(AuthentikProviderRadius instance,
    Input<string> authorizationFlow, Input<string> invalidationFlow, Input<string>? authenticationFlow);

  public static partial ProviderSsfArgs CreateProvider(AuthentikProviderSsf instance, Input<string> authorizationFlow,
    Input<string> invalidationFlow, Input<string>? authenticationFlow);

  public static partial ProviderScimArgs CreateProvider(AuthentikProviderScim instance, Input<string> authorizationFlow,
    Input<string> invalidationFlow, Input<string>? authenticationFlow);

  public static partial ProviderMicrosoftEntraArgs CreateProvider(AuthentikProviderMicrosoftEntra instance,
    Input<string> authorizationFlow, Input<string> invalidationFlow, Input<string>? authenticationFlow);

  public static partial ProviderGoogleWorkspaceArgs CreateProvider(AuthentikProviderGoogleWorkspace instance,
    Input<string> authorizationFlow, Input<string> invalidationFlow, Input<string>? authenticationFlow);

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


  public static string ResourceName(ClusterApplicationResources.Args args, ApplicationDefinition resource) =>
    PostfixName( $"{Prefix(args, resource)}-{resource.Metadata.Name}");

  private static string Prefix(ClusterApplicationResources.Args args, ApplicationDefinition resource) =>
    resource.Namespace() is { } ns && ns == args.ClusterName ? args.ClusterName : $"{args.ClusterName}-{resource.Namespace()}";

  private static Input<string> MapToStringInput(string value) => value;
  private static Input<bool>? MapToBoolInput(bool? value) => value.HasValue ? (Input<bool>?)value : null;
  private static Input<int>? MapToIntInput(int? value) => value.HasValue ? (Input<int>?)value : null;

  public static string PostfixName(string name) => OperatingSystem.IsLinux() ? name : $"{name}-test";
}
