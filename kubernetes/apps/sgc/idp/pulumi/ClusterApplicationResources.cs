using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dumpify;
using Humanizer;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Pulumi;
using Pulumi.Authentik;
using StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public class ClusterApplicationResources : ComponentResource
{
  public class Args : ResourceArgs
  {
    public required string ClusterName { get; init; }
    public required string ClusterTitle { get; init; }
    public required Kubernetes RemoteCluster { get; init; }
    public required Kubernetes UptimeCluster { get; init; }
    public required ServiceConnectionKubernetes ServiceConnection { get; init; }
  }
  public ClusterApplicationResources(string name, Args args,
    ComponentResourceOptions? options = null) : base("custom:resource:ClusterApplicationResources", name, args, options)
  {
    var outpostProviders = Output.Create<ImmutableArray<double>>([]);
    var applications = Output.Create(GetApplications(args.RemoteCluster))
      .Apply(async applications =>
      {
        var monitors = new List<KumaResource>();
        var missingMonitors = (await args.UptimeCluster.CustomObjects.ListNamespacedCustomObjectAsync<KumaResourceList>(
          "autokuma.bigboot.dev", "v1", "observability", "kumaentities", labelSelector: $"driscoll.dev/cluster={args.ClusterName}")).Items.Select(z => z.Metadata.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        await foreach (var app in applications)
        {
          var spec = app.Spec;
          if (spec is { Authentik: { } authentik })
          {
            var authentikApp = CreateAuthentikApplication(args, app, authentik);
            outpostProviders = Output.Tuple(outpostProviders, authentikApp.ProtocolProvider)
              .Apply(a => a.Item2.HasValue ? a.Item1.Add(a.Item2.Value) : a.Item1);
          }

          if (app.Spec.Uptime is { })
          {
            // var monitor = new Monitor(Mappings.ResourceName(clusterName, app), Mappings.MapMonitorArgs(app));
            var monitor = Mappings.MapMonitor(args.ClusterName, app);
            missingMonitors.Remove(monitor.Metadata.Name);
            monitors.Add(monitor);
          }
        }
        foreach (var monitorName in missingMonitors)
        {
          // If the monitor is not found, we delete it from the uptime cluster.
          await args.UptimeCluster.CustomObjects.DeleteNamespacedCustomObjectAsync("autokuma.bigboot.dev", "v1", "observability", "kumaentities", monitorName);
        }

        foreach (var monitor in monitors)
        {
          await UpdateMonitor(args, monitor);
        }

        return applications;
      });


    _ = outpostProviders.Apply(outpostProviders =>
    {
      outpostProviders.Dump();
      if (outpostProviders.Any())
      {
        var outpost = new Outpost($"ak-outpost-{args.ClusterName}", new()
        {
          ServiceConnection = args.ServiceConnection.Id,
          Type = "proxy",
          Name = $"Outpost for {args.ClusterTitle}",
          Config = Output.JsonSerialize(Output.Create(new
          {
            object_naming_template = $"ak-outpost-{args.ClusterName}",
            kubernetes_replicas = 2,
            kubernetes_namespace = args.ClusterName,
            kubernetes_ingress_class_name = "internal"
          })),
          ProtocolProviders = outpostProviders
        }, new CustomResourceOptions() { Parent = this });
      }

      return outpostProviders;
    });
  }

  static async IAsyncEnumerable<ApplicationDefinition> GetApplications(Kubernetes clusterClient)
  {
    var namespaces = await clusterClient.ListNamespaceAsync();

    foreach (var ns in namespaces.Items)
    {
      var result = await clusterClient.CustomObjects.ListNamespacedCustomObjectAsync<ApplicationDefinitionList>(
        "driscoll.dev", "v1", ns.Metadata.Name, "applicationdefinitions");
      foreach (var definition in result.Items)
      {
        var spec = definition.Spec;
        if (spec is { AuthentikFrom: { } authentikFrom })
        {
          if (definition.Spec.AuthentikFrom is { Type: "configMap", Name: var configMapName })
          {
            var configMap =
              await clusterClient.CoreV1.ReadNamespacedConfigMapAsync(configMapName, definition.Namespace());
            spec = spec with
            {
              Authentik = JsonSerializer.Deserialize<Authentik>(JsonSerializer.Serialize(configMap.Data))
            };
          }
          else if (definition.Spec.AuthentikFrom is { Type: "secret", Name: var secretName })
          {
            var secret = await clusterClient.CoreV1.ReadNamespacedSecretAsync(secretName, definition.Namespace());
            var data = secret.Data.ToDictionary(kvp => kvp.Key,
              kvp => System.Text.Encoding.UTF8.GetString(kvp.Value));
            spec = spec with
            {
              Authentik = JsonSerializer.Deserialize<Authentik>(JsonSerializer.Serialize(data))
            };
          }
          else
          {
            throw new ArgumentException($"Unknown AuthentikFrom type: {authentikFrom.Type}");
          }
        }

        if (spec is { UptimeFrom: { } uptimeFrom })
        {
          if (uptimeFrom is { Type: "configMap", Name: var configMapName })
          {
            var configMap =
              await clusterClient.CoreV1.ReadNamespacedConfigMapAsync(configMapName, definition.Namespace());
            spec = spec with
            {
              Uptime = Mappings.MapFromConfigMap(configMap)
            };
          }
          else if (uptimeFrom is { Type: "secret", Name: var secretName })
          {
            var secret = await clusterClient.CoreV1.ReadNamespacedSecretAsync(secretName, definition.Namespace());
            spec = spec with
            {
              Uptime = Mappings.MapFromSecret(secret)
            };
          }
          else
          {
            throw new ArgumentException($"Unknown UptimeFrom type: {uptimeFrom.Type}");
          }
        }

        definition.Spec = spec;
        yield return definition;
      }
    }
  }

  static async Task UpdateMonitor(Args args, KumaResource applicationMonitor)
  {
    try
    {
      var existingEntity = await args.UptimeCluster.CustomObjects.GetNamespacedCustomObjectAsync<KumaResource>(
        "autokuma.bigboot.dev",
        "v1",
        "observability",
        "kumaentities",
        applicationMonitor.Metadata.Name);
      // existingEntity.Spec.Dump("existingEntity");
    }
    catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      if (Deployment.Instance.IsDryRun) return;
      await args.UptimeCluster.CustomObjects.CreateNamespacedCustomObjectAsync(applicationMonitor,
        "autokuma.bigboot.dev",
        "v1",
        "observability", "kumaentities");
      return;
    }

    try
    {
      var remoteEntity = await args.RemoteCluster.CustomObjects.GetNamespacedCustomObjectAsync<KumaResource>(
        "autokuma.bigboot.dev",
        "v1",
        applicationMonitor.Namespace(),
        "kumaentities",
        applicationMonitor.Metadata.Name);
      // remoteEntity.Dump("remoteEntity");
    }
    catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      if (Deployment.Instance.IsDryRun) return;
      // If the remote entity does not exist, we delete it from the uptime cluster.
      await args.UptimeCluster.CustomObjects.DeleteNamespacedCustomObjectAsync("autokuma.bigboot.dev", "v1",
        "observability",
        "kumaentities", applicationMonitor.Metadata.Name);
      return;
    }

    if (Deployment.Instance.IsDryRun) return;
    await args.UptimeCluster.CustomObjects.PatchNamespacedCustomObjectAsync(applicationMonitor, "autokuma.bigboot.dev",
      "v1",
      applicationMonitor.Namespace(), "observability", "kumaentities");
  }

  Application CreateAuthentikApplication(Args args,
    ApplicationDefinition definition, Authentik authentik)
  {
    var providers = new List<CustomResource>();
    var slug = authentik.Slug ?? $"{args.ClusterName}-{definition.Spec.Name.Dasherize()}";
    var resourceName = Mappings.ResourceName(args.ClusterName, definition);
    CustomResource provider = authentik.Provider.ToLowerInvariant() switch
    {
      "oauth2" => new ProviderOauth2(resourceName, Mappings.MapFromResourceArgs(new ProviderOauth2Args()
      {
        Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        ClientId = authentik.Config["clientId"],
        ClientSecret = authentik.Config["clientSecret"],
        ClientType = "confidential",
        AuthorizationFlow = Defaults.Flows.ProviderAuthorizationImplicitConsent.Apply(f => f.Id),
        InvalidationFlow = Defaults.Flows.InvalidationFlow.Apply(f => f.Id),
        // todo
      }, authentik.Config), new CustomResourceOptions() { Parent = this }),
      "forward-auth" => new ProviderProxy(resourceName, Mappings.MapFromResourceArgs(new ProviderProxyArgs()
      {
        Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        Mode = "forward_single",
        ExternalHost = authentik.Url,
        AuthorizationFlow = Defaults.Flows.ProviderAuthorizationImplicitConsent.Apply(f => f.Id),
        InvalidationFlow = Defaults.Flows.InvalidationFlow.Apply(f => f.Id),
      }, authentik.Config), new CustomResourceOptions() { Parent = this }),
      "proxy" => new ProviderProxy(resourceName, Mappings.MapFromResourceArgs(new ProviderProxyArgs()
      {
        Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        Mode = "proxy",
        ExternalHost = authentik.Url,
        AuthorizationFlow = Defaults.Flows.ProviderAuthorizationImplicitConsent.Apply(f => f.Id),
        InvalidationFlow = Defaults.Flows.InvalidationFlow.Apply(f => f.Id),
      }, authentik.Config), new CustomResourceOptions() { Parent = this }),
      _ => throw new ArgumentException($"Unknown provider: {authentik.Provider}")
    };
    providers.Add(provider);

    return new Application(resourceName, new()
    {
      // ApplicationId = ,
      ProtocolProvider = provider.Id.Apply(id => double.Parse(id)),
      Name = definition.Spec.Name,
      Slug = slug,
      Group = definition.Spec.Category,
      MetaIcon = authentik.Icon,
      MetaPublisher = args.ClusterTitle,
      MetaDescription = definition.Spec.Description ?? "",
      MetaLaunchUrl = authentik.Url,
      // PolicyEngineMode = "any",
      // OpenInNewTab = true,
    }, new CustomResourceOptions() { Parent = this });
  }

}
