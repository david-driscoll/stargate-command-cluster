using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using authentik.Models;
using Humanizer;
using k8s;
using k8s.Models;
using Pulumi;
using Pulumi.Authentik;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using CustomResource = Pulumi.Kubernetes.ApiExtensions.CustomResource;
using Provider = Pulumi.Kubernetes.Provider;

public class ClusterApplicationResources : ComponentResource
{
  private readonly Mappings _mapper;
  private readonly IDictionary<string, string> _resourceNames = new Dictionary<string, string>();

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
    name, args, options)
  {
    _mapper = new Mappings(args, _resourceNames);
    var kumaResources = new List<(ApplicationDefinition app, KumaUptimeResourceConfigArgs config)>();
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
            // ya ya state...
            _mapper.ResourceName(app);
            kumaResources.Add((app, _mapper.MapMonitor(app)));
          }
        }

        return (kubernetesProvider, applications);
      });


    _ = outpostProviders.Apply(outpostProviders =>
    {
      if (outpostProviders.Any())
      {
        var outpostName = $"ak-outpost-{args.ClusterName}";
        var outpost = new Outpost(outpostName, new()
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

    _ = applications.Apply(a =>
    {
      var kubernetesProvider = a.kubernetesProvider;
      foreach (var (app, config) in kumaResources)
      {
        var resourceName = _mapper.ResourceName(app);
        if (config.ParentName is { })
        {
          config.ParentName = config.ParentName.Apply(parentName =>
          {
            if (!_resourceNames.TryGetValue(parentName, out var resourceName))
              return parentName;
            return resourceName;
          });
        }
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
          Spec = new KumaUptimeResourceSpecArgs { Config = config }
        }, new CustomResourceOptions() { Parent = this, Provider = kubernetesProvider });
      }

      return a.kubernetesProvider;
    });
  }

  Output<(Provider Provider, ImmutableList<ApplicationDefinition> Applications)> GetApplications(
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
              "saml" => new ApplicationDefinitionAuthentik { ProviderSaml = _mapper.MapToSaml(authentikSpec) },
              "oauth2" => new ApplicationDefinitionAuthentik { ProviderOauth2 = _mapper.MapToOauth2(authentikSpec) },
              "scim" => new ApplicationDefinitionAuthentik { ProviderScim = _mapper.MapToScim(authentikSpec) },
              "ssf" => new ApplicationDefinitionAuthentik { ProviderSsf = _mapper.MapToSsf(authentikSpec) },
              "proxy" => new ApplicationDefinitionAuthentik { ProviderProxy = _mapper.MapToProxy(authentikSpec) },
              "radius" => new ApplicationDefinitionAuthentik { ProviderRadius = _mapper.MapToRadius(authentikSpec) },
              "rac" => new ApplicationDefinitionAuthentik { ProviderRac = _mapper.MapToRac(authentikSpec) },
              "ldap" => new ApplicationDefinitionAuthentik { ProviderLdap = _mapper.MapToLdap(authentikSpec) },
              "microsoftEntra" => new ApplicationDefinitionAuthentik
                { ProviderMicrosoftEntra = _mapper.MapToMicrosoftEntra(authentikSpec) },
              "googleWorkspace" => new ApplicationDefinitionAuthentik
              {
                ProviderGoogleWorkspace = _mapper.MapToGoogleWorkspace(authentikSpec)
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
    var slug = definition.Spec.Slug ?? ($"{args.ClusterName}-{definition.Spec.Name}").Dehumanize().Underscore().Dasherize();
    var resourceName = _mapper.ResourceName(definition);
    var options = new CustomResourceOptions() { Parent = this };
    Pulumi.CustomResource provider;
    switch (authentik)
    {
      case { ProviderProxy: { } proxy }:
      {
        var providerArgs = new ProviderProxyArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        };
        _mapper.MapProviderArgs(providerArgs, args);
        _mapper.MapProviderArgs(providerArgs, proxy);
        provider = new ProviderProxy(resourceName, providerArgs, options);
        break;
      }
      case { ProviderOauth2: { } oauth2 }:
      {
        var providerArgs = new ProviderOauth2Args()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        };
        _mapper.MapProviderArgs(providerArgs, args);
        _mapper.MapProviderArgs(providerArgs, oauth2);
        provider = new ProviderOauth2(resourceName, providerArgs, options);
        break;
      }
      case { ProviderLdap: { } ldap }:
      {
        var providerArgs = new ProviderLdapArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        };
        _mapper.MapProviderArgs(providerArgs, args);
        _mapper.MapProviderArgs(providerArgs, ldap);
        provider = new ProviderLdap(resourceName, providerArgs, options);
        break;
      }
      case { ProviderSaml: { } saml }:
      {
        var providerArgs = new ProviderSamlArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        };
        _mapper.MapProviderArgs(providerArgs, args);
        _mapper.MapProviderArgs(providerArgs, saml);
        provider = new ProviderSaml(resourceName, providerArgs, options);
        break;
      }
      case { ProviderRac: { } rac }:
      {
        var providerArgs = new ProviderRacArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        };
        _mapper.MapProviderArgs(providerArgs, args);
        _mapper.MapProviderArgs(providerArgs, rac);
        provider = new ProviderRac(resourceName, providerArgs, options);
        break;
      }
      case { ProviderRadius: { } radius }:
      {
        var providerArgs = new ProviderRadiusArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        };
        _mapper.MapProviderArgs(providerArgs, args);
        _mapper.MapProviderArgs(providerArgs, radius);
        provider = new ProviderRadius(resourceName, providerArgs, options);
        break;
      }
      case { ProviderSsf: { } ssf }:
      {
        var providerArgs = new ProviderSsfArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        };
        _mapper.MapProviderArgs(providerArgs, args);
        _mapper.MapProviderArgs(providerArgs, ssf);
        provider = new ProviderSsf(resourceName, providerArgs, options);
        break;
      }
      case { ProviderScim: { } scim }:
      {
        var providerArgs = new ProviderScimArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        };
        _mapper.MapProviderArgs(providerArgs, args);
        _mapper.MapProviderArgs(providerArgs, scim);
        provider = new ProviderScim(resourceName, providerArgs, options);
        break;
      }
      case { ProviderMicrosoftEntra: { } microsoftEntra }:
      {
        var providerArgs = new ProviderMicrosoftEntraArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        };
        _mapper.MapProviderArgs(providerArgs, args);
        _mapper.MapProviderArgs(providerArgs, microsoftEntra);
        provider = new ProviderMicrosoftEntra(resourceName, providerArgs, options);
        break;
      }
      case { ProviderGoogleWorkspace: { } googleWorkspace }:
      {
        var providerArgs = new ProviderGoogleWorkspaceArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({args.ClusterTitle})"),
        };
        _mapper.MapProviderArgs(providerArgs, args);
        _mapper.MapProviderArgs(providerArgs, googleWorkspace);
        provider = new ProviderGoogleWorkspace(resourceName, providerArgs, options);
        break;
      }
      default:
        throw new ArgumentException("Unknown authentik provider type", nameof(authentik));
    }

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
