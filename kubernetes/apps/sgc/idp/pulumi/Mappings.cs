using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using applications.AuthentikResources;
using applications.Models;
using applications.Models.ApplicationDefinition;
using applications.Models.Authentik;
using applications.Models.UptimeKuma;
using k8s;
using k8s.Models;
using Pulumi;
using Pulumi.Authentik;
using Riok.Mapperly.Abstractions;

namespace applications;

[Mapper(AllowNullPropertyAssignment = false)]
static partial class Mappings
{
  internal static async Task<ImmutableList<ApplicationDefinition>> GetApplications(Kubernetes client)
  {
    var builder = ImmutableList.CreateBuilder<ApplicationDefinition>();
    foreach (var ns in (await client.ListNamespaceAsync()).Items)
    foreach (var entity in (await client.CustomObjects.ListNamespacedCustomObjectAsync<ApplicationDefinitionList>(
               "driscoll.dev", "v1", ns.Metadata.Name, "applicationdefinitions")).Items)
    {
      entity.Metadata.Annotations ??= new Dictionary<string, string>();
      entity.Metadata.Labels ??= new Dictionary<string, string>();
      entity.Metadata.Labels.TryAdd("driscoll.dev/namespace", entity.Metadata.Namespace());
      entity.Metadata.Labels.TryAdd("driscoll.dev/cluster", "sgc");
      entity.Metadata.Annotations.TryAdd("driscoll.dev/clusterTitle", "Stargate Command");
      builder.Add(await PopulateCluster.MapApplicationDefinition(client, entity));
    }

    return builder.ToImmutable();
  }

  public static partial void MapProviderArgs([MappingTarget] ProviderProxyArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderProxyArgs args, AuthentikProviderProxy instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderOauth2Args args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderOauth2Args args, AuthentikProviderOauth2 instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderSamlArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderSamlArgs args, AuthentikProviderSaml instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderLdapArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderLdapArgs args, AuthentikProviderLdap instance);

  public static partial void MapProviderArgs([MappingTarget] SourceSamlArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] SourceSamlArgs args, AuthentikProviderSaml instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderRacArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderRacArgs args, AuthentikProviderRac instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderRadiusArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderRadiusArgs args, AuthentikProviderRadius instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderSsfArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderSsfArgs args, AuthentikProviderSsf instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderScimArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderScimArgs args, AuthentikProviderScim instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderMicrosoftEntraArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderMicrosoftEntraArgs args,
    AuthentikProviderMicrosoftEntra instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderGoogleWorkspaceArgs args,
    AuthentikApplicationResources.ClusterFlows instance);

  public static partial void MapProviderArgs([MappingTarget] ProviderGoogleWorkspaceArgs args,
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

  public static string ResourceName(ApplicationDefinition resource)
  {
    return $"{Prefix(resource)}-{resource.Metadata.Name}";
  }

  private static string Prefix(ApplicationDefinition resource)
  {
    var (clusterName, _, ns) = resource.GetClusterNameAndTitle();
    return ns == clusterName ? clusterName : $"{clusterName}-{resource.Namespace()}";
  }

  private static Input<string> MapToStringInput(string value) => value;
  private static InputList<string> MapToStringInput(ImmutableList<string> value) => [..value];
  private static InputList<double> MapToDoubleInput(ImmutableList<double> value) => [..value];
  private static Input<bool>? MapToBoolInput(bool? value) => value.HasValue ? (Input<bool>?)value : null;
  private static Input<int>? MapToIntInput(int? value) => value.HasValue ? (Input<int>?)value : null;
}
