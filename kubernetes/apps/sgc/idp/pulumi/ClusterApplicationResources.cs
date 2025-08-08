using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using authentik.Models;
using Dumpify;
using Humanizer;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Pulumi;
using Pulumi.Authentik;
using Pulumi.Kubernetes;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using CustomResource = Pulumi.Kubernetes.ApiExtensions.CustomResource;
using Provider = Pulumi.Kubernetes.Provider;
using ProviderArgs = Pulumi.Authentik.ProviderArgs;

public class ClusterApplicationResources : ComponentResource
{
  public class Args : ResourceArgs
  {
    public required string ClusterName { get; set; }
    public required string ClusterTitle { get; set; }
    public required Input<(Kubernetes Client, Pulumi.Kubernetes.Provider Provider)> RemoteCluster { get; set; }
    public required Input<(Kubernetes Client, Pulumi.Kubernetes.Provider Provider)> UptimeCluster { get; set; }
    public required ServiceConnectionKubernetes ServiceConnection { get; set; }
    public required Input<string> AuthorizationFlow { get; set; }
    public Input<string>? AuthenticationFlow { get; set; }
    public required Input<string> InvalidationFlow { get; set; }
  }

  public ClusterApplicationResources(string name, Args args,
    ComponentResourceOptions? options = null) : base("custom:resource:ClusterApplicationResources",
    Mappings.PostfixName(name), args, options)
  {
    var outpostProviders = Output.Create<ImmutableArray<double>>([]);
    var applications = GetApplications(args.RemoteCluster)
      .Apply(data =>
      {
        var (kubernetesProvider, applications) = data;
        foreach (var app in applications)
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
            var resourceName = Mappings.PostfixName(Mappings.ResourceName(args, app));
            _ = new CustomResource(resourceName, new KumaUptimeResourceArgs()
            {
              Metadata = new ObjectMetaArgs()
              {
                Name = resourceName,
                Namespace = "observability",
                Labels = new Dictionary<string, string>
                {
                  ["driscoll.dev/cluster"] = args.ClusterName
                }
              },
              Spec = new KumaUptimeResourceSpecArgs { Config = Mappings.MapMonitor(args.ClusterName, app) }
            }, new CustomResourceOptions() { Parent = this, Provider = kubernetesProvider });
          }
        }

        return applications;
      });


    _ = outpostProviders.Apply(outpostProviders =>
    {
      if (outpostProviders.Any())
      {
        var outpostName = Mappings.PostfixName($"ak-outpost-{args.ClusterName}");
        var outpost = new Outpost(outpostName, new()
        {
          ServiceConnection = args.ServiceConnection.Id,
          Type = "proxy",
          Name = $"Outpost for {Mappings.PostfixName(args.ClusterTitle)}",
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

  static Output<(Provider Provider, ImmutableList<ApplicationDefinition> Applications)> GetApplications(
    Input<(Kubernetes Client, Provider Provider)> input)
  {
    return input.Apply(async x =>
    {
      var (clusterClient, provider) = x;

      var namespaces = await clusterClient.ListNamespaceAsync();
      var builder = ImmutableList.CreateBuilder<ApplicationDefinition>();

      foreach (var ns in namespaces.Items)
      {
        var result = await clusterClient.CustomObjects.ListNamespacedCustomObjectAsync<ApplicationDefinitionList>(
          "driscoll.dev", "v1", ns.Metadata.Name, "applicationdefinitions");
        // new { Namespace = ns.Metadata.Name, result.Items }.Dump();
        foreach (var definition in result.Items)
        {
          var spec = definition.Spec;
          if (spec is { AuthentikFrom: { } authentikFrom })
          {
            IDictionary<string, string> data;
            if (authentikFrom is { Type: "configMap", Name: var configMapName })
            {
              var configMap =
                await clusterClient.CoreV1.ReadNamespacedConfigMapAsync(configMapName, definition.Namespace());
              data = configMap.Data;
            }
            else if (authentikFrom is { Type: "secret", Name: var secretName })
            {
              var secret = await clusterClient.CoreV1.ReadNamespacedSecretAsync(secretName, definition.Namespace());
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
              "saml" => new ApplicationDefinitionAuthentik { ProviderSaml = Mappings.MapToSaml(authentikSpec) },
              "oauth2" => new ApplicationDefinitionAuthentik { ProviderOauth2 = Mappings.MapToOauth2(authentikSpec) },
              "scim" => new ApplicationDefinitionAuthentik { ProviderScim = Mappings.MapToScim(authentikSpec) },
              "ssf" => new ApplicationDefinitionAuthentik { ProviderSsf = Mappings.MapToSsf(authentikSpec) },
              "proxy" => new ApplicationDefinitionAuthentik { ProviderProxy = Mappings.MapToProxy(authentikSpec) },
              "radius" => new ApplicationDefinitionAuthentik { ProviderRadius = Mappings.MapToRadius(authentikSpec) },
              "rac" => new ApplicationDefinitionAuthentik { ProviderRac = Mappings.MapToRac(authentikSpec) },
              "ldap" => new ApplicationDefinitionAuthentik { ProviderLdap = Mappings.MapToLdap(authentikSpec) },
              "microsoftEntra" => new ApplicationDefinitionAuthentik
                { ProviderMicrosoftEntra = Mappings.MapToMicrosoftEntra(authentikSpec) },
              "googleWorkspace" => new ApplicationDefinitionAuthentik
              {
                ProviderGoogleWorkspace = Mappings.MapToGoogleWorkspace(authentikSpec)
              },
              _ => throw new ArgumentException($"Unknown Authentik provider type: {authentikSpec.Type}",
                nameof(authentikSpec))
            };
            spec = spec with { Authentik = authentik };
          }

          if (spec is { UptimeFrom: { } uptimeFrom })
          {
            IDictionary<string, string> data;
            if (uptimeFrom is { Type: "configMap", Name: var configMapName })
            {
              var configMap =
                await clusterClient.CoreV1.ReadNamespacedConfigMapAsync(configMapName, definition.Namespace());
              data = configMap.Data;
            }
            else if (uptimeFrom is { Type: "secret", Name: var secretName })
            {
              var secret = await clusterClient.CoreV1.ReadNamespacedSecretAsync(secretName, definition.Namespace());
              data = secret.Data.ToDictionary(kvp => kvp.Key,
                kvp => System.Text.Encoding.UTF8.GetString(kvp.Value));
            }
            else
            {
              throw new ArgumentException($"Unknown AuthentikFrom type: {uptimeFrom.Type}");
            }

            spec = spec with { Uptime = Mappings.MapFromUptimeData(data) };
          }

          definition.Spec = spec;
          builder.Add(definition);
        }
      }

      return (provider, builder.ToImmutableList());
    });
  }

  Application CreateAuthentikApplication(Args args, ApplicationDefinition definition,
    ApplicationDefinitionAuthentik authentik)
  {
    var slug = Mappings.PostfixName(definition.Spec.Slug ?? $"{args.ClusterName}-{definition.Spec.Name.Dasherize()}");
    var resourceName = Mappings.PostfixName(Mappings.ResourceName(args, definition));
    var options = new CustomResourceOptions() { Parent = this };
    var providerArgs = new ProviderProxyArgs()
    {
      Name = definition.Spec.Name,
    };
    Pulumi.CustomResource authentikProvider;
    switch (authentik)
    {
      case { ProviderProxy: { } proxy }:

        Mappings.MapProviderArgs(providerArgs, proxy, args.AuthorizationFlow, args.InvalidationFlow,
          args.AuthenticationFlow);
        break;
      case { ProviderOauth2: { } oauth2 }:

          Mappings.MapProviderArgs(providerArgs, oauth2, args.AuthorizationFlow, args.InvalidationFlow,
            args.AuthenticationFlow);
        break;
      case { ProviderLdap: { } ldap }:

          Mappings.MapProviderArgs(providerArgs, ldap, args.AuthorizationFlow, args.InvalidationFlow,
            args.AuthenticationFlow);
        break;
      case { ProviderSaml: { } saml }:

          Mappings.MapProviderArgs(providerArgs, saml, args.AuthorizationFlow, args.InvalidationFlow,
            args.AuthenticationFlow);
        break;
      case { ProviderRac: { } rac }:

          Mappings.MapProviderArgs(providerArgs, rac, args.AuthorizationFlow, args.InvalidationFlow,
            args.AuthenticationFlow);
        break;
      case { ProviderRadius: { } radius }:
          Mappings.MapProviderArgs(providerArgs, radius, args.AuthorizationFlow, args.InvalidationFlow,
            args.AuthenticationFlow);
        break;
      case { ProviderSsf: { } ssf }:
          Mappings.MapProviderArgs(providerArgs, ssf, args.AuthorizationFlow, args.InvalidationFlow,
            args.AuthenticationFlow);
        break;
      case { ProviderScim: { } scim }:
          Mappings.MapProviderArgs(providerArgs, scim, args.AuthorizationFlow, args.InvalidationFlow,
            args.AuthenticationFlow);
        break;
      case { ProviderMicrosoftEntra: { } microsoftEntra }:
          Mappings.MapProviderArgs(providerArgs, microsoftEntra, args.AuthorizationFlow, args.InvalidationFlow,
            args.AuthenticationFlow);
        break;
      case { ProviderGoogleWorkspace: { } googleWorkspace }:
          Mappings.MapProviderArgs(providerArgs, googleWorkspace, args.AuthorizationFlow, args.InvalidationFlow,
            args.AuthenticationFlow);
        break;
      default:
        throw new ArgumentException("Unknown authentik provider type", nameof(authentik));
    }

    var provider = new ProviderProxy(resourceName, providerArgs, options);

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
