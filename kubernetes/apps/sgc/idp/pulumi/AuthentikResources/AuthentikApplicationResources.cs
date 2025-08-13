using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using applications.Models;
using applications.Models.ApplicationDefinition;
using Humanizer;
using k8s;
using Pulumi;
using Pulumi.Authentik;

namespace applications.AuthentikResources;

public class AuthentikApplicationResources : ComponentResource
{
  public class Args : ResourceArgs
  {
    public required Kubernetes Cluster { get; init; }

    // public required ServiceConnectionKubernetes ServiceConnection { get; init; }
  }

  public AuthentikApplicationResources(Args args,
    ComponentResourceOptions? options = null) : base("custom:resource:AuthentikApplicationResources",
    "authentik-applications", args, options)
  {
    var outposts = Output.Create(ImmutableDictionary<string, OutpostArgs>.Empty);
    var applications = Output.Create(Mappings.GetApplications(args.Cluster))
      .Apply(applications => applications
        .Where(z => z.Spec.Authentik is not null)
        .Select(x => CreateResource(args, x, ref outposts)));

    var createdOutposts = outposts.Apply(x => x
      .Select(z => new Outpost(z.Key, z.Value, new CustomResourceOptions() { Parent = this }))
      .ToImmutableArray());
  }

  private Application CreateResource(Args args, ApplicationDefinition application,
    ref Output<ImmutableDictionary<string, OutpostArgs>> outposts)
  {
    Debug.Assert(application.Spec.Authentik != null);

    var (clusterName, clusterTitle, ns) = application.GetClusterNameAndTitle();
    var authentikApp = CreateAuthentikApplication(args, application, application.Spec.Authentik);
    outposts = Output.Tuple(outposts, authentikApp.ProtocolProvider).Apply(async x =>
    {
      var (outposts, protocolProvider) = x;
      if (protocolProvider is null or <= 0)
      {
        return outposts;
      }

      if (outposts.TryGetValue(clusterName, out var outpost))
      {
        outpost.ProtocolProviders.Add(protocolProvider);
        return outposts;
      }

      ServiceConnectionKubernetes serviceConnection;
      if (clusterName == "sgc")
      {
        serviceConnection = new ServiceConnectionKubernetes(clusterName, new ServiceConnectionKubernetesArgs()
        {
          ServiceConnectionKubernetesId = clusterName,
          Name = clusterTitle,
          Local = true,
          VerifySsl = true,
        });
      }
      else
      {
        var kubeConfig = await args.Cluster.ReadNamespacedSecretAsync($"{clusterName}-kubeconfig", "sgc");
        serviceConnection = new ServiceConnectionKubernetes(clusterName, new ServiceConnectionKubernetesArgs()
        {
          ServiceConnectionKubernetesId = clusterName,
          Name = clusterTitle,
          Kubeconfig = Encoding.UTF8.GetString(kubeConfig.Data["kubeconfig.json"]),
          VerifySsl = true,
        });
      }

      outpost = new()
      {
        ServiceConnection = serviceConnection.ServiceConnectionKubernetesId,
        Type = "proxy",
        Name = $"Outpost for {clusterTitle}",
        Config = Output.JsonSerialize(Output.Create(new
        {
          object_naming_template = $"ak-outpost-{clusterName}",
          kubernetes_replicas = 2,
          kubernetes_namespace = clusterName,
          kubernetes_ingress_class_name = "internal"
        })),
        ProtocolProviders = []
      };
      outposts = outposts.Add(clusterName, outpost);

      outpost.ProtocolProviders.Add(protocolProvider);
      return outposts;
    });

    return authentikApp;
  }


  Application CreateAuthentikApplication(Args args, ApplicationDefinition definition,
    ApplicationDefinitionAuthentik authentik)
  {
    var (clusterName, clusterTitle, ns) = definition.GetClusterNameAndTitle();
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
        Mappings.MapProviderArgs(providerArgs, args);
        Mappings.MapProviderArgs(providerArgs, proxy);
        provider = new ProviderProxy(resourceName, providerArgs, options);
        break;
      }
      case { ProviderOauth2: { } oauth2 }:
      {
        var providerArgs = new ProviderOauth2Args()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
        };
        Mappings.MapProviderArgs(providerArgs, args);
        Mappings.MapProviderArgs(providerArgs, oauth2);
        provider = new ProviderOauth2(resourceName, providerArgs, options);
        break;
      }
      case { ProviderLdap: { } ldap }:
      {
        var providerArgs = new ProviderLdapArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
        };
        Mappings.MapProviderArgs(providerArgs, args);
        Mappings.MapProviderArgs(providerArgs, ldap);
        provider = new ProviderLdap(resourceName, providerArgs, options);
        break;
      }
      case { ProviderSaml: { } saml }:
      {
        var providerArgs = new ProviderSamlArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
        };
        Mappings.MapProviderArgs(providerArgs, args);
        Mappings.MapProviderArgs(providerArgs, saml);
        provider = new ProviderSaml(resourceName, providerArgs, options);
        break;
      }
      case { ProviderRac: { } rac }:
      {
        var providerArgs = new ProviderRacArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
        };
        Mappings.MapProviderArgs(providerArgs, args);
        Mappings.MapProviderArgs(providerArgs, rac);
        provider = new ProviderRac(resourceName, providerArgs, options);
        break;
      }
      case { ProviderRadius: { } radius }:
      {
        var providerArgs = new ProviderRadiusArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
        };
        Mappings.MapProviderArgs(providerArgs, args);
        Mappings.MapProviderArgs(providerArgs, radius);
        provider = new ProviderRadius(resourceName, providerArgs, options);
        break;
      }
      case { ProviderSsf: { } ssf }:
      {
        var providerArgs = new ProviderSsfArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
        };
        Mappings.MapProviderArgs(providerArgs, args);
        Mappings.MapProviderArgs(providerArgs, ssf);
        provider = new ProviderSsf(resourceName, providerArgs, options);
        break;
      }
      case { ProviderScim: { } scim }:
      {
        var providerArgs = new ProviderScimArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
        };
        Mappings.MapProviderArgs(providerArgs, args);
        Mappings.MapProviderArgs(providerArgs, scim);
        provider = new ProviderScim(resourceName, providerArgs, options);
        break;
      }
      case { ProviderMicrosoftEntra: { } microsoftEntra }:
      {
        var providerArgs = new ProviderMicrosoftEntraArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
        };
        Mappings.MapProviderArgs(providerArgs, args);
        Mappings.MapProviderArgs(providerArgs, microsoftEntra);
        provider = new ProviderMicrosoftEntra(resourceName, providerArgs, options);
        break;
      }
      case { ProviderGoogleWorkspace: { } googleWorkspace }:
      {
        var providerArgs = new ProviderGoogleWorkspaceArgs()
        {
          Name = Output.Format($"Provider for {definition.Spec.Name} ({clusterTitle})"),
        };
        Mappings.MapProviderArgs(providerArgs, args);
        Mappings.MapProviderArgs(providerArgs, googleWorkspace);
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
