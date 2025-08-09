using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using authentik.Models;
using Humanizer;
using k8s;
using k8s.Models;
using Pulumi;
using Pulumi.Authentik;
using Riok.Mapperly.Abstractions;

[Mapper(AllowNullPropertyAssignment = false)]
partial class Mappings
{
  private readonly ClusterApplicationResources.Args _args;
  private readonly IDictionary<string, string> _resourceNames;

  public Mappings(ClusterApplicationResources.Args args, IDictionary<string, string> resourceNames)
  {
    _args = args;
    _resourceNames = resourceNames;
  }

  public KumaUptimeResourceConfigArgs MapMonitor(ApplicationDefinition definition)
  {
    Debug.Assert(definition.Spec.Uptime != null, "definition.Uptime != null");
    var args = new KumaUptimeResourceConfigArgs
    {
      Name = PostfixTitle(definition.Spec.Name),
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

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, DnsUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, HttpUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PingUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, DockerUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, GamedigUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, GroupUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, GrpcKeywordUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, JsonQueryUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, KafkaProducerUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, KeywordUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, MongoDbUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, MqttUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, MysqlUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PortUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PostgresUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PushUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, RadiusUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, RealBrowserUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, RedisUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, SteamUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, SqlServerUptime uptime);

  [MapProperty("ParentName", nameof(KumaUptimeResourceConfigArgs.ParentName), Use = nameof(MapToParentName))]
  public partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, TailscalePingUptime uptime);

  public partial AuthentikProviderSaml MapToSaml(AuthentikSpec spec);
  public partial AuthentikProviderOauth2 MapToOauth2(AuthentikSpec spec);
  public partial AuthentikProviderScim MapToScim(AuthentikSpec spec);
  public partial AuthentikProviderSsf MapToSsf(AuthentikSpec spec);
  public partial AuthentikProviderProxy MapToProxy(AuthentikSpec spec);
  public partial AuthentikProviderRadius MapToRadius(AuthentikSpec spec);
  public partial AuthentikProviderRac MapToRac(AuthentikSpec spec);
  public partial AuthentikProviderLdap MapToLdap(AuthentikSpec spec);
  public partial AuthentikProviderMicrosoftEntra MapToMicrosoftEntra(AuthentikSpec spec);
  public partial AuthentikProviderGoogleWorkspace MapToGoogleWorkspace(AuthentikSpec spec);

  public partial void MapProviderArgs([MappingTarget] ProviderProxyArgs args,
    ClusterApplicationResources.Args instance);

  public partial void MapProviderArgs([MappingTarget] ProviderProxyArgs args, AuthentikProviderProxy instance);

  public partial void MapProviderArgs([MappingTarget] ProviderOauth2Args args,
    ClusterApplicationResources.Args instance);

  public partial void MapProviderArgs([MappingTarget] ProviderOauth2Args args, AuthentikProviderOauth2 instance);
  public partial void MapProviderArgs([MappingTarget] ProviderSamlArgs args, ClusterApplicationResources.Args instance);
  public partial void MapProviderArgs([MappingTarget] ProviderSamlArgs args, AuthentikProviderSaml instance);

  public partial void MapProviderArgs([MappingTarget] ProviderLdapArgs args, ClusterApplicationResources.Args instance);
  public partial void MapProviderArgs([MappingTarget] ProviderLdapArgs args, AuthentikProviderLdap instance);

  public partial void MapProviderArgs([MappingTarget] SourceSamlArgs args, ClusterApplicationResources.Args instance);
  public partial void MapProviderArgs([MappingTarget] SourceSamlArgs args, AuthentikProviderSaml instance);

  public partial void MapProviderArgs([MappingTarget] ProviderRacArgs args, ClusterApplicationResources.Args instance);
  public partial void MapProviderArgs([MappingTarget] ProviderRacArgs args, AuthentikProviderRac instance);

  public partial void MapProviderArgs([MappingTarget] ProviderRadiusArgs args,
    ClusterApplicationResources.Args instance);

  public partial void MapProviderArgs([MappingTarget] ProviderRadiusArgs args, AuthentikProviderRadius instance);

  public partial void MapProviderArgs([MappingTarget] ProviderSsfArgs args, ClusterApplicationResources.Args instance);
  public partial void MapProviderArgs([MappingTarget] ProviderSsfArgs args, AuthentikProviderSsf instance);

  public partial void MapProviderArgs([MappingTarget] ProviderScimArgs args, ClusterApplicationResources.Args instance);
  public partial void MapProviderArgs([MappingTarget] ProviderScimArgs args, AuthentikProviderScim instance);

  public partial void MapProviderArgs([MappingTarget] ProviderMicrosoftEntraArgs args,
    ClusterApplicationResources.Args instance);

  public partial void MapProviderArgs([MappingTarget] ProviderMicrosoftEntraArgs args,
    AuthentikProviderMicrosoftEntra instance);

  public partial void MapProviderArgs([MappingTarget] ProviderGoogleWorkspaceArgs args,
    ClusterApplicationResources.Args instance);

  public partial void MapProviderArgs([MappingTarget] ProviderGoogleWorkspaceArgs args,
    AuthentikProviderGoogleWorkspace instance);

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


  public string ResourceName(ApplicationDefinition resource)
  {
    return _resourceNames[resource.Metadata.Name] = $"{Prefix(resource.Namespace())}-{resource.Metadata.Name}";
  }

  private string Prefix(string @namespace) =>
    @namespace is { } ns && ns == _args.ClusterName
      ? _args.ClusterName
      : $"{_args.ClusterName}-{@namespace}";

  private static Input<string> MapToStringInput(string value) => value;
  private static InputList<string> MapToStringInput(ImmutableList<string> value) => [..value];
  private static InputList<double> MapToDoubleInput(ImmutableList<double> value) => [..value];
  private static Input<bool>? MapToBoolInput(bool? value) => value.HasValue ? (Input<bool>?)value : null;
  private static Input<int>? MapToIntInput(int? value) => value.HasValue ? (Input<int>?)value : null;

  [UserMapping(Default = false)]
  private Input<string>? MapToParentName(string? value)
  {
    if (value is null)
      return null;

    return Output.Create(value)
      .Apply(parentName => !_resourceNames.TryGetValue(parentName, out var resourceName) ? parentName : resourceName);
  }

  [UserMapping(Ignore = true)]
  public static string PostfixName(string name) => (OperatingSystem.IsLinux() ? name : $"{name}-test")
    .ToLowerInvariant().Dehumanize().Underscore().Dasherize();

  [UserMapping(Ignore = true)]
  public static string PostfixTitle(string name) => OperatingSystem.IsLinux() ? name : $"[Test] {name}";
}
