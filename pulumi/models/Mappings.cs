using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using applications.Models.Authentik;
using applications.Models.UptimeKuma;
using k8s;
using k8s.Models;
using models.Applications;
using Pulumi;
using Riok.Mapperly.Abstractions;

namespace models;

[Mapper(AllowNullPropertyAssignment = false)]
public static partial class Mappings
{
  public static async IAsyncEnumerable<string> GetExternalDnsRecords(Kubernetes client)
  {
    var ingresses = await client.ListIngressForAllNamespacesAsync();
    foreach (var host in ingresses.Items.SelectMany(z => z.Spec.Rules)
               .Select(rule => rule.Host)
               .Distinct())
    {
      yield return host;
    }
  }

  public static async IAsyncEnumerable<ClusterDefinition> GetClusters(Kubernetes client)
  {
    var results =
      await client.ListClusterCustomObjectAsync<ClusterDefinitionList>("driscoll.dev", "v1", "clusterdefinitions");
    foreach (var item in results.Items)
      yield return item;
  }

  public static async IAsyncEnumerable<ApplicationDefinition> GetApplications(Kubernetes client)
  {
    foreach (var ns in (await client.ListNamespaceAsync()).Items)
    foreach (var entity in (await client.CustomObjects.ListNamespacedCustomObjectAsync<ApplicationDefinitionList>(
               "driscoll.dev", "v1", ns.Metadata.Name, "applicationdefinitions")).Items)
    {
      entity.Metadata.Annotations ??= new Dictionary<string, string>();
      entity.Metadata.Labels ??= new Dictionary<string, string>();
      entity.Metadata.Labels.TryAdd("driscoll.dev/namespace", entity.Metadata.Namespace());
      entity.Metadata.Labels.TryAdd("driscoll.dev/cluster", "sgc");
      entity.Metadata.Annotations.TryAdd("driscoll.dev/originalName", entity.Metadata.Name);
      entity.Metadata.Annotations.TryAdd("driscoll.dev/clusterTitle", "Stargate Command");
      yield return await MapApplicationDefinition(client, entity);
    }
  }

  public static async ValueTask<ApplicationDefinition> MapApplicationDefinition(Kubernetes remoteCluster,
    ApplicationDefinition resource)
  {
    var spec = resource.Spec;
    var (_, _, ns, originalName) = resource.GetClusterNameAndTitle();
    if (spec is { AuthentikFrom: { } authentikFrom })
    {
      IDictionary<string, string> data;
      if (authentikFrom is { Type: "configMap", Name: var configMapName })
      {
        var configMap =
          await remoteCluster.CoreV1.ReadNamespacedConfigMapAsync(configMapName, ns);
        data = configMap.Data;
      }
      else if (authentikFrom is { Type: "secret", Name: var secretName })
      {
        var secret = await remoteCluster.CoreV1.ReadNamespacedSecretAsync(secretName, ns);
        data = secret.Data.ToDictionary(kvp => kvp.Key,
          kvp => System.Text.Encoding.UTF8.GetString(kvp.Value));
      }
      else
      {
        throw new ArgumentException($"Unknown AuthentikFrom type: {authentikFrom.Type}");
      }


      var authentikSpec = KubernetesJson.Deserialize<AuthentikSpec>(KubernetesJson.Serialize(data));

      ApplicationDefinitionAuthentik authentik = authentikSpec.Type switch
      {
        "saml" => new ApplicationDefinitionAuthentik { ProviderSaml = ModelMappings.MapToSaml(authentikSpec) },
        "oauth2" => new ApplicationDefinitionAuthentik { ProviderOauth2 = ModelMappings.MapToOauth2(authentikSpec) },
        "scim" => new ApplicationDefinitionAuthentik { ProviderScim = ModelMappings.MapToScim(authentikSpec) },
        "ssf" => new ApplicationDefinitionAuthentik { ProviderSsf = ModelMappings.MapToSsf(authentikSpec) },
        "proxy" => new ApplicationDefinitionAuthentik { ProviderProxy = ModelMappings.MapToProxy(authentikSpec) },
        "radius" => new ApplicationDefinitionAuthentik { ProviderRadius = ModelMappings.MapToRadius(authentikSpec) },
        "rac" => new ApplicationDefinitionAuthentik { ProviderRac = ModelMappings.MapToRac(authentikSpec) },
        "ldap" => new ApplicationDefinitionAuthentik { ProviderLdap = ModelMappings.MapToLdap(authentikSpec) },
        "microsoftEntra" => new ApplicationDefinitionAuthentik
          { ProviderMicrosoftEntra = ModelMappings.MapToMicrosoftEntra(authentikSpec) },
        "googleWorkspace" => new ApplicationDefinitionAuthentik
        {
          ProviderGoogleWorkspace = ModelMappings.MapToGoogleWorkspace(authentikSpec)
        },
        _ => throw new ArgumentException($"Unknown Authentik provider type: {authentikSpec.Type}",
          nameof(authentikSpec))
      };
      spec = spec with { Authentik = authentik, AuthentikFrom = null };
    }

    if (spec is { UptimeFrom: { } uptimeFrom })
    {
      IDictionary<string, string> data;
      if (uptimeFrom is { Type: "configMap", Name: var configMapName })
      {
        var configMap =
          await remoteCluster.CoreV1.ReadNamespacedConfigMapAsync(configMapName, ns);
        data = configMap.Data;
      }
      else if (uptimeFrom is { Type: "secret", Name: var secretName })
      {
        var secret = await remoteCluster.CoreV1.ReadNamespacedSecretAsync(secretName, ns);
        data = secret.Data.ToDictionary(kvp => kvp.Key,
          kvp => System.Text.Encoding.UTF8.GetString(kvp.Value));
      }
      else
      {
        throw new ArgumentException($"Unknown AuthentikFrom type: {uptimeFrom.Type}");
      }

      spec = spec with { Uptime = ModelMappings.MapFromUptimeData(data), UptimeFrom = null };
    }

    resource.Spec = spec;
    return resource;
  }


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
    var (clusterName, _, ns, _) = resource.GetClusterNameAndTitle();
    return ns == clusterName ? clusterName : $"{clusterName}-{resource.Namespace()}";
  }


  private static Input<string> MapToStringInput(string value) => value;
  private static InputList<string> MapToStringInput(ImmutableList<string> value) => [..value];
  private static InputList<double> MapToDoubleInput(ImmutableList<double> value) => [..value];
  private static Input<bool>? MapToBoolInput(bool? value) => value.HasValue ? (Input<bool>?)value : null;
  private static Input<int>? MapToIntInput(int? value) => value.HasValue ? (Input<int>?)value : null;
}
