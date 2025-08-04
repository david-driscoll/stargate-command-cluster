using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
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
          IDictionary<string, string> data;
          if (definition.Spec.AuthentikFrom is { Type: "configMap", Name: var configMapName })
          {
            var configMap =
              await clusterClient.CoreV1.ReadNamespacedConfigMapAsync(configMapName, definition.Namespace());
            data = configMap.Data;
          }
          else if (definition.Spec.AuthentikFrom is { Type: "secret", Name: var secretName })
          {
            var secret = await clusterClient.CoreV1.ReadNamespacedSecretAsync(secretName, definition.Namespace());
            data = secret.Data.ToDictionary(kvp => kvp.Key,
              kvp => System.Text.Encoding.UTF8.GetString(kvp.Value));
          }
          else
          {
            throw new ArgumentException($"Unknown AuthentikFrom type: {authentikFrom.Type}");
          }
          var authentikSpec = JsonSerializer.Deserialize<AuthentikSpec>(JsonSerializer.Serialize(data));

          ApplicationDefinitionAuthentik authentik = authentikSpec.Type switch
          {
            "saml" => new ApplicationDefinitionAuthentik { ProviderSaml = Mappings.MapToSaml(authentikSpec) },
            "oauth2" => new ApplicationDefinitionAuthentik { ProviderOauth2 = Mappings.MapToOauth2(authentikSpec) },
            "scim" => new ApplicationDefinitionAuthentik { ProviderScim = Mappings.MapToScim(authentikSpec) },
            "ssf" => new ApplicationDefinitionAuthentik { ProviderSsf = Mappings.MapToSsf(authentikSpec) },
            "proxy" => new ApplicationDefinitionAuthentik { ProviderProxy = Mappings.MapToProxy(authentikSpec) },
            "radius" => new ApplicationDefinitionAuthentik { ProviderRadius = Mappings.MapToRadius(authentikSpec) },
            "rac" => new ApplicationDefinitionAuthentik { ProviderRac = Mappings.MapToRac(authentikSpec) },
            "ldap" => new ApplicationDefinitionAuthentik { ProviderLdap = Mappings.MapToLdap(authentikSpec) },
            "microsoftEntra" => new ApplicationDefinitionAuthentik { ProviderMicrosoftEntra = Mappings.MapToMicrosoftEntra(authentikSpec) },
            "googleWorkspace" => new ApplicationDefinitionAuthentik { ProviderGoogleWorkspace = Mappings.MapToGoogleWorkspace(authentikSpec)
            },
            _ => throw new ArgumentException($"Unknown Authentik provider type: {authentikSpec.Type}",
              nameof(authentikSpec))
          };
          spec = spec with { Authentik = authentik };
        }

        if (spec is { UptimeFrom: { } uptimeFrom })
        {
          if (uptimeFrom is { Type: "configMap", Name: var configMapName })
          {
            var configMap =
              await clusterClient.CoreV1.ReadNamespacedConfigMapAsync(configMapName, definition.Namespace());
            spec = spec with { Uptime = Mappings.MapFromConfigMap(configMap) };
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
    ApplicationDefinition definition, ApplicationDefinitionAuthentik authentik)
  {
    var slug = definition.Spec.Slug ?? $"{args.ClusterName}-{definition.Spec.Name.Dasherize()}";
    var resourceName = Mappings.ResourceName(args.ClusterName, definition);
    CustomResource provider = authentik switch
    {
      { ProviderProxy: { } proxy } => new ProviderProxy(resourceName, Mappings.CreateProvider(proxy),
        new CustomResourceOptions() { Parent = this }),
      { ProviderOauth2: { } oauth2 } => new ProviderOauth2(resourceName, Mappings.CreateProvider(oauth2),
        new CustomResourceOptions() { Parent = this }),
      { ProviderLdap: { } ldap } => new ProviderLdap(resourceName, Mappings.CreateProvider(ldap),
        new CustomResourceOptions() { Parent = this }),
      { ProviderSaml: { } saml } => new ProviderSaml(resourceName, Mappings.CreateProvider(saml),
        new CustomResourceOptions() { Parent = this }),
      { ProviderRac: { } oidc } => new ProviderRac(resourceName, Mappings.CreateProvider(oidc),
        new CustomResourceOptions() { Parent = this }),
      { ProviderRadius: { } oidc } => new ProviderRadius(resourceName, Mappings.CreateProvider(oidc),
        new CustomResourceOptions() { Parent = this }),
      { ProviderSsf: { } oidc } => new ProviderSsf(resourceName, Mappings.CreateProvider(oidc),
        new CustomResourceOptions() { Parent = this }),
      { ProviderScim: { } scim } => new ProviderScim(resourceName, Mappings.CreateProvider(scim),
        new CustomResourceOptions() { Parent = this }),
      { ProviderMicrosoftEntra: { } microsoftEntra } => new ProviderMicrosoftEntra(resourceName,
        Mappings.CreateProvider(microsoftEntra), new CustomResourceOptions() { Parent = this }),
      { ProviderGoogleWorkspace: { } googleWorkspace } => new ProviderGoogleWorkspace(resourceName,
        Mappings.CreateProvider(googleWorkspace), new CustomResourceOptions() { Parent = this }),
      _ => throw new ArgumentException("Unknown authentik provider type", nameof(authentik))
    };

    return new Application(resourceName, new()
    {
      // ApplicationId = ,
      ProtocolProvider = provider.Id.Apply(double.Parse),
      Name = definition.Spec.Name,
      Slug = slug,
      Group = definition.Spec.Category,
      MetaIcon = definition.Spec.Icon,
      MetaPublisher = args.ClusterTitle,
      MetaDescription = definition.Spec.Description ?? "",
      MetaLaunchUrl = definition.Spec.Url,
      // PolicyEngineMode = "any",
      // OpenInNewTab = true,
    }, new CustomResourceOptions() { Parent = this });
  }

}
