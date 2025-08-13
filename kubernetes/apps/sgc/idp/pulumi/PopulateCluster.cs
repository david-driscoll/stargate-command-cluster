using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using applications;
using Dumpify;
using k8s;
using k8s.Models;
using Models;
using Models.ApplicationDefinition;
using Models.Authentik;

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
          "driscoll.dev", "v1", "applicationdefinitions", z => z.Metadata.Name, MapApplicationDefinition);
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
    foreach (var entity in (await remoteCluster.CustomObjects.ListClusterCustomObjectAsync<TList>(group, version,
               plural)).Items)
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


    var remoteEntities = remoteEntitiesBuilder.ToImmutable();
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
    resources.Dump(title);
  }

  internal static async ValueTask<ApplicationDefinition> MapApplicationDefinition(Kubernetes remoteCluster,
    ApplicationDefinition resource)
  {
    var spec = resource.Spec;
    var (_, _, ns) = resource.GetClusterNameAndTitle();
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
}
