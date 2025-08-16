using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Humanizer;
using k8s;
using k8s.Models;
using models;
using models.Applications;
using Pulumi;
using Pulumi.Authentik;
using ModelMappings = models.ModelMappings;

namespace applications.AuthentikResources;

public class AuthentikApplicationResources : ComponentResource
{
  public class ClusterFlows()
  {
    public required Input<string> AuthorizationFlow { get; init; }
    public Input<string>? AuthenticationFlow { get; init; }
    public required Input<string> InvalidationFlow { get; init; }
  }
  public class Args : ResourceArgs
  {
    public required ImmutableDictionary<string, ClusterFlows> ClusterFlows { get; init; }
    public required Kubernetes Cluster { get; init; }
    public required ImmutableDictionary<string, ClusterDefinition> ClusterInfo { get; set; }

    // public required ServiceConnectionKubernetes ServiceConnection { get; init; }
  }

  public AuthentikApplicationResources(Args args,
    ComponentResourceOptions? options = null) : base("custom:resource:AuthentikApplicationResources",
    "authentik-applications", args, options)
  {
    var applications = Output.Create(Mappings.GetApplications(args.Cluster))
      .Apply(applications => applications
        .Where(z => z.Spec.Authentik is not null)
        .GroupBy(z => z.GetClusterNameAndTitle().ClusterName))
      .Apply(async groups =>
      {
        await foreach (var applications in groups)
        {
          var apps = applications.Select(app => CreateResource(args, app)).ToImmutableArray();
          if (apps.Length == 0)
          {
            continue;
          }

          var (clusterName, clusterTitle, ns) = applications.First().GetClusterNameAndTitle();
          ServiceConnectionKubernetes serviceConnection;
          if (applications.Key == "sgc")
          {
            serviceConnection = new ServiceConnectionKubernetes(clusterName, new ServiceConnectionKubernetesArgs()
            {
              Name = clusterTitle,
              Local = true,
              VerifySsl = true,
            }, new CustomResourceOptions() { Parent = this });
          }
          else
          {
            var kubeConfig = await args.Cluster.ReadNamespacedSecretAsync($"{clusterName}-authentik-outpost-kubeconfig", "sgc");
            serviceConnection = new ServiceConnectionKubernetes(clusterName, new ServiceConnectionKubernetesArgs()
            {
              Name = clusterTitle,
              Kubeconfig = Encoding.UTF8.GetString(kubeConfig.Data["kubeconfig.json"]),
              VerifySsl = true,
            }, new CustomResourceOptions() { Parent = this });
          }

          var clusterInfo = args.ClusterInfo[clusterName];

          var outpost = new Outpost(clusterName, new OutpostArgs()
          {
            ServiceConnection = serviceConnection.ServiceConnectionKubernetesId,
            Type = "proxy",
            Name = $"Outpost for {clusterTitle}",
            Config = Output.JsonSerialize(Output.Create(new
            {
              authentik_host = "https://authentik.driscoll.tech/",
              authentik_host_insecure = false,
              authentik_host_browser = clusterInfo.Spec.Domain,
              log_level = "info",
              object_naming_template = $"ak-outpost-{clusterName}",
              kubernetes_replicas = 2,
              kubernetes_namespace = clusterName,
              kubernetes_ingress_class_name = "internal",
            })),
            ProtocolProviders = [.. apps.Select(app => app.ProtocolProvider.Apply(z => z.Value))],
          }, new CustomResourceOptions() { Parent = this });
        }

      });
  }

  private Application CreateResource(Args args, ApplicationDefinition application)
  {
    Log.Info($"Creating authentik application for {application.Metadata.Name} in {application.Metadata.Namespace()}");
    Debug.Assert(application.Spec.Authentik != null);

    var (clusterName, clusterTitle, ns) = ModelMappings.GetClusterNameAndTitle(application);
    var authentikApp = CreateAuthentikApplication(args, application, application.Spec.Authentik);
    return authentikApp;
  }


  Application CreateAuthentikApplication(Args args, ApplicationDefinition definition,
    ApplicationDefinitionAuthentik authentik)
  {
    var (clusterName, clusterTitle, ns) = ModelMappings.GetClusterNameAndTitle(definition);
    var clusterFlows = args.ClusterFlows[clusterName];
    var slug = definition.Spec.Slug ??
               $"{clusterName}-{definition.Spec.Name}".Dehumanize().Underscore().Dasherize();
    var resourceName = Mappings.ResourceName(definition);
    var options = new CustomResourceOptions() { Parent = this };
    Pulumi.CustomResource provider;
    switch (authentik)
    {
      case { ProviderProxy: { } proxy }:
        {
          var providerArgs = new ProviderProxyArgs()
          {
            Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
          };
          FlowMappings.MapProviderArgs(providerArgs, clusterFlows);
          FlowMappings.MapProviderArgs(providerArgs, proxy);
          provider = new ProviderProxy(resourceName, providerArgs, options);
          break;
        }
      case { ProviderOauth2: { } oauth2 }:
        {
          var providerArgs = new ProviderOauth2Args()
          {
            Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
          };
          FlowMappings.MapProviderArgs(providerArgs, clusterFlows);
          FlowMappings.MapProviderArgs(providerArgs, oauth2);
          provider = new ProviderOauth2(resourceName, providerArgs, options);
          break;
        }
      case { ProviderLdap: { } ldap }:
        {
          var providerArgs = new ProviderLdapArgs()
          {
            Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
          };
          FlowMappings.MapProviderArgs(providerArgs, clusterFlows);
          FlowMappings.MapProviderArgs(providerArgs, ldap);
          provider = new ProviderLdap(resourceName, providerArgs, options);
          break;
        }
      case { ProviderSaml: { } saml }:
        {
          var providerArgs = new ProviderSamlArgs()
          {
            Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
          };
          FlowMappings.MapProviderArgs(providerArgs, clusterFlows);
          FlowMappings.MapProviderArgs(providerArgs, saml);
          provider = new ProviderSaml(resourceName, providerArgs, options);
          break;
        }
      case { ProviderRac: { } rac }:
        {
          var providerArgs = new ProviderRacArgs()
          {
            Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
          };
          FlowMappings.MapProviderArgs(providerArgs, clusterFlows);
          FlowMappings.MapProviderArgs(providerArgs, rac);
          provider = new ProviderRac(resourceName, providerArgs, options);
          break;
        }
      case { ProviderRadius: { } radius }:
        {
          var providerArgs = new ProviderRadiusArgs()
          {
            Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
          };
          FlowMappings.MapProviderArgs(providerArgs, clusterFlows);
          FlowMappings.MapProviderArgs(providerArgs, radius);
          provider = new ProviderRadius(resourceName, providerArgs, options);
          break;
        }
      case { ProviderSsf: { } ssf }:
        {
          var providerArgs = new ProviderSsfArgs()
          {
            Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
          };
          FlowMappings.MapProviderArgs(providerArgs, clusterFlows);
          FlowMappings.MapProviderArgs(providerArgs, ssf);
          provider = new ProviderSsf(resourceName, providerArgs, options);
          break;
        }
      case { ProviderScim: { } scim }:
        {
          var providerArgs = new ProviderScimArgs()
          {
            Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
          };
          FlowMappings.MapProviderArgs(providerArgs, clusterFlows);
          FlowMappings.MapProviderArgs(providerArgs, scim);
          provider = new ProviderScim(resourceName, providerArgs, options);
          break;
        }
      case { ProviderMicrosoftEntra: { } microsoftEntra }:
        {
          var providerArgs = new ProviderMicrosoftEntraArgs()
          {
            Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
          };
          FlowMappings.MapProviderArgs(providerArgs, clusterFlows);
          FlowMappings.MapProviderArgs(providerArgs, microsoftEntra);
          provider = new ProviderMicrosoftEntra(resourceName, providerArgs, options);
          break;
        }
      case { ProviderGoogleWorkspace: { } googleWorkspace }:
        {
          var providerArgs = new ProviderGoogleWorkspaceArgs()
          {
            Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
          };
          FlowMappings.MapProviderArgs(providerArgs, clusterFlows);
          FlowMappings.MapProviderArgs(providerArgs, googleWorkspace);
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
      MetaPublisher = clusterTitle,
      MetaDescription = definition.Spec.Description ?? "",
      MetaLaunchUrl = definition.Spec.Url,
      // PolicyEngineMode = "any",
      // OpenInNewTab = true,
    }, new CustomResourceOptions() { Parent = this });
  }
}
