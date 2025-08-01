#!/usr/bin/dotnet run
// #:package YamlDotNet@16.3.0
#:package gstocco.YamlDotNet.YamlPath@1.0.26
#:package Spectre.Console@0.50.0
#:package Spectre.Console.Json@0.50.0
#:package KubernetesClient@*
#:package Microsoft.Extensions.Logging@9.*
#:package Dumpify@0.6.6
#:package Lunet.Extensions.Logging.SpectreConsole@1.2.0
#:property JsonSerializerIsReflectionEnabledByDefault=true
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Dumpify;
using k8s;
using k8s.Models;
using Lunet.Extensions.Logging.SpectreConsole;
using Microsoft.Extensions.Logging;

var factory = LoggerFactory.Create(configure =>
           {
             configure.AddSpectreConsole(new SpectreConsoleLoggerOptions()
             {
               IncludeNewLineBeforeMessage = false,
               IncludeTimestamp = true,
             });
           }
       );

const string rootDomain = "${ROOT_DOMAIN}";
var comparer = EqualityComparer<KumaResource>.Create((x, y) => x!.Metadata.Name == y!.Metadata.Name && x!.Metadata.Namespace == y!.Metadata.Namespace, x => HashCode.Combine(x.Metadata.Name, x.Metadata.Namespace));

var sgcConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile("/config/${APP}-${CLUSTER_CNAME}-kubeconfig");
var equestriaConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile("/config/${APP}-equestria-kubeconfig");

var sgcClient = new Kubernetes(sgcConfig);
var equestriaClient = new Kubernetes(equestriaConfig);
await UpdateCluster("equestria", comparer, sgcClient, equestriaClient);

static void DumpNames(string title, IEnumerable<KumaResource> resources)
{
  resources.Select(MapName).Dump(title);
}

static string MapName(KumaResource resource) => $"{resource.Metadata.Namespace()}@{resource.Metadata.Name}";

static async Task UpdateCluster(string cluster, EqualityComparer<KumaResource> comparer, Kubernetes sgcCluster, Kubernetes remoteCluster)
{
  var existingEntities = (await sgcCluster.CustomObjects.ListClusterCustomObjectAsync<KumaResourceList>("autokuma.bigboot.dev", "v1", "kumaentities", labelSelector: $"{rootDomain}.cluster={cluster}")).Items
    .ToImmutableArray();
  DumpNames("existingEntities", existingEntities);

  var remoteEntities = (await remoteCluster.CustomObjects.ListClusterCustomObjectAsync<KumaResourceList>("autokuma.bigboot.dev", "v1", "kumaentities")).Items
      .Select(MapRemoteEntity(cluster))
  .ToImmutableArray();
  DumpNames("remoteEntities", remoteEntities);

  // Both remoteEntities and existingEntities are ImmutableHashSet<KumaResource> and using Except with a custom comparer is correct and efficient.
  var missingRemoteEntities = remoteEntities.ExceptBy(existingEntities.Select(MapName), MapName);
  var removedRemoteEntities = existingEntities.ExceptBy(remoteEntities.Select(MapName), MapName);

  DumpNames("missingRemoteEntities", missingRemoteEntities);
  DumpNames("removedRemoteEntities", removedRemoteEntities);

  foreach (var missingEntity in missingRemoteEntities)
  {
    await sgcCluster.CustomObjects.CreateNamespacedCustomObjectAsync(missingEntity, "autokuma.bigboot.dev", "v1", "observability", "kumaentities");
  }

  foreach (var removedEntity in removedRemoteEntities)
  {
    await sgcCluster.CustomObjects.DeleteNamespacedCustomObjectAsync("autokuma.bigboot.dev", "v1", "observability", "kumaentities", removedEntity.Metadata.Name);
  }
}

static Func<KumaResource, KumaResource> MapRemoteEntity(string cluster) => resource =>
{
  var prefix = cluster;
  if (resource.Metadata.Namespace() is { } ns && !ns.Equals(cluster, StringComparison.OrdinalIgnoreCase))
  {
    prefix = $"{cluster}-{ns}";
  }
  resource.Metadata.Name = $"{prefix}-{resource.Metadata.Name}";
  resource.Metadata.SetNamespace("observability");
  resource.Metadata.Labels ??= new Dictionary<string, string>();
  resource.Metadata.Labels[$"{rootDomain}.cluster"] = cluster;
  resource.Metadata.ResourceVersion = null;
  return resource;
};
public class KumaResource : KubernetesObject, IMetadata<V1ObjectMeta>
{
  [JsonPropertyName("metadata")]
  public V1ObjectMeta Metadata { get; set; }

  [JsonPropertyName("spec")]
  public object Spec { get; set; }

  [JsonPropertyName("status")]
  public object Status { get; set; }
}

public class KumaResourceList : KubernetesObject
{
  public V1ListMeta Metadata { get; set; }
  public List<KumaResource> Items { get; set; }
}

