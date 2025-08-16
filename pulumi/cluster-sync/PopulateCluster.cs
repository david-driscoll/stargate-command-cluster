using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dumpify;
using k8s;
using k8s.Models;
using models;
using models.Applications;

namespace applications;

public static class PopulateCluster
{
  const string destinationNamespace = "observability";

  public static async Task PopulateClusters(Kubernetes localCluster)
  {
    var clusters =
      (await localCluster.ListClusterCustomObjectAsync<ClusterDefinitionList>("driscoll.dev", "v1",
        "clusterdefinitions")).Items.ToImmutableArray();
    foreach (var cluster in clusters)
    {
      if (string.IsNullOrWhiteSpace(cluster.Spec.Secret))
      {
        continue;
      }
      try
      {
        await SyncEntityWithCluster<ApplicationDefinitionList, ApplicationDefinition>(localCluster, cluster,
          "driscoll.dev", "v1", "applicationdefinitions", z => z.Metadata.Name, Mappings.MapApplicationDefinition);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error syncing cluster {cluster.Metadata.Name}: {ex.Message}");
        ex.Dump();
      }
    }
  }

  static async Task SyncEntityWithCluster<TList, TResult>(Kubernetes localCluster, ClusterDefinition cluster,
    string group, string version, string plural, Func<TResult, string> getName,
    Func<Kubernetes, TResult, ValueTask<TResult>> mapEntity)
    where TList : IKubernetesList<TResult>
    where TResult : KubernetesObject, IMetadata<V1ObjectMeta>, IKubernetesSpec
  {
    var externalSecret = await localCluster.ReadNamespacedSecretAsync(cluster.Spec.Secret, "sgc");
    var config =
      await KubernetesClientConfiguration.LoadKubeConfigAsync(new MemoryStream(externalSecret.Data["kubeconfig.json"]));
    var remoteCluster = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigObject(config));

    var existingEntities = (await localCluster.CustomObjects.ListNamespacedCustomObjectAsync<TList>(group, version,
        destinationNamespace, plural, labelSelector: $"driscoll.dev/cluster={cluster.Metadata.Name}")).Items
      .ToImmutableArray();
    DumpNames("existingEntities", existingEntities.Select(getName));

    var remoteEntitiesBuilder = ImmutableList.CreateBuilder<TResult>();
    foreach (var ns in (await remoteCluster.ListNamespaceAsync()).Items)
    foreach (var entity in (await remoteCluster.CustomObjects.ListNamespacedCustomObjectAsync<TList>(group, version, ns.Namespace(), plural)).Items)
    {
      entity.Metadata.Annotations ??= new Dictionary<string, string>();
      entity.Metadata.Labels ??= new Dictionary<string, string>();
      entity.Metadata.Labels["driscoll.dev/cluster"] = cluster.Metadata.Name;
      entity.Metadata.Annotations["driscoll.dev/clusterTitle"] = cluster.Spec.Name;
      entity.Metadata.Labels["driscoll.dev/namespace"] = entity.Metadata.Namespace();
      entity.Metadata.Name = $"{Prefix(entity)}-{entity.Metadata.Name}";
      entity.Metadata.SetNamespace(destinationNamespace);
      entity.Metadata.ResourceVersion = null;
      remoteEntitiesBuilder.Add(await mapEntity(remoteCluster, entity));
    }

    static string Prefix(TResult resource)
    {
      var (clusterName, _, ns) = resource.GetClusterNameAndTitle();
      return ns == clusterName ? clusterName : $"{clusterName}-{resource.Namespace()}";
    }

    var remoteEntities = remoteEntitiesBuilder.DistinctBy(getName).ToImmutableList();
    DumpNames("remoteEntities", remoteEntities.Select(getName));

    // Both remoteEntities and existingEntities are ImmutableHashSet<KumaResource> and using Except with a custom comparer is correct and efficient.
    var missingRemoteEntities = remoteEntities.ExceptBy(existingEntities.Select(getName), getName);
    var removedRemoteEntities = existingEntities.ExceptBy(remoteEntities.Select(getName), getName);

    DumpNames("missingRemoteEntities", missingRemoteEntities.Select(getName));
    DumpNames("removedRemoteEntities", removedRemoteEntities.Select(getName));

    foreach (var removedEntity in removedRemoteEntities)
    {
      await localCluster.CustomObjects.DeleteNamespacedCustomObjectAsync(group, version, destinationNamespace, plural,
        removedEntity.Metadata.Name);
    }

    foreach (var missingEntity in missingRemoteEntities)
    {
      await localCluster.CustomObjects.CreateNamespacedCustomObjectAsync(missingEntity, group, version,
        destinationNamespace, plural);
    }

    foreach (var remoteEntity in remoteEntities)
    {
      await localCluster.CustomObjects.PatchNamespacedCustomObjectAsync<TResult>(
        new V1Patch($$"""
                      {"spec": {{KubernetesJson.Serialize(remoteEntity.Spec)}} }
                      """, V1Patch.PatchType.MergePatch), group, version, destinationNamespace, plural, remoteEntity.Metadata.Name);
    }
  }

  static void DumpNames(string title, IEnumerable<string> resources)
  {
    resources.Distinct().Dump(title);
  }
}
