using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using applications;
using applications.AuthentikResources;
using applications.KumaResources;
using applications.Models.ApplicationDefinition;
using Dumpify;
using k8s;
using Microsoft.Extensions.DependencyInjection;
using Pulumi;
using Pulumi.Authentik;
using Spectre.Console;

KubernetesJson.AddJsonOptions(options => { options.Converters.Add(new YamlMemberConverterFactory()); });

return await Deployment.RunAsync(async () =>
{
  Kubernetes cluster;
  {
    static Kubernetes CreateClientAndProvider(string kubeConfig, string name, string? context = null)
    {
      using var stream = new MemoryStream(Encoding.ASCII.GetBytes(kubeConfig));
      var config = KubernetesClientConfiguration.LoadKubeConfig(stream);
      var clientConfig = KubernetesClientConfiguration.BuildConfigFromConfigObject(config);
      var client = new Kubernetes(clientConfig);
      if (context != null)
      {
        config.CurrentContext = context;
      }

      return client;
    }

    if (OperatingSystem.IsLinux())
    {
      cluster = new Kubernetes(KubernetesClientConfiguration.InClusterConfig());
    }
    else
    {
      var kubeConfig = File.ReadAllText(KubernetesClientConfiguration.KubeConfigDefaultLocation);
      cluster = CreateClientAndProvider(kubeConfig, "sgc", "admin@sgc");
    }

    try
    {
      await PopulateCluster.PopulateClusters(cluster);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error populating cluster: {ex.Message}");
      ex.Dump();
    }
  }

  var policies = new Policies();
  var stages = new Stages();
  _ = new AuthentikGroups();
  var kumaGroups = new KumaGroups();

  _ = new KumaUptimeResources(new()
  {
    Cluster = cluster,
    Groups = kumaGroups,
  });

  var tailscaleSource = new SourceOauth("tailscale", new()
  {
    Name = "Tailscale",
    Slug = "tailscale",
    ProviderType = "openidconnect",

    Enabled = true,
    AuthenticationFlow = null,
    EnrollmentFlow = null,

    PolicyEngineMode = "any",
    UserPathTemplate = "driscoll.dev/tailscale/%(slug)s",

    OidcWellKnownUrl = "https://idp.opossum-yo.ts.net/.well-known/openid-configuration",
    ConsumerKey = "unused",
    ConsumerSecret = "unused",
    UserMatchingMode = "email_link",
    GroupMatchingMode = "name_link",
  });

  var discordSource = new SourceOauth("discord", new()
  {
    Name = "Discord",
    Slug = "discord",
    ProviderType = "discord",

    Enabled = true,
    AuthenticationFlow = null,
    EnrollmentFlow = null,

    PolicyEngineMode = "any",
    UserPathTemplate = "driscoll.dev/discord/%(slug)s",

    ConsumerKey = "unused",
    ConsumerSecret = "unused",
    UserMatchingMode = "email_link",
    GroupMatchingMode = "name_link",

    AdditionalScopes = "guilds guilds.members.read",
  });

  var clusters =
    (await cluster.ListClusterCustomObjectAsync<ClusterDefinitionList>("driscoll.dev", "v1",
      "clusterdefinitions")).Items.ToImmutableArray();
  var clusterFlows = ImmutableDictionary.CreateBuilder<string, AuthentikApplicationResources.ClusterFlows>();
  foreach (var definition in clusters)
  {
    var flows = new Flows(policies, stages, definition);
    clusterFlows[definition.Metadata.Name] = new AuthentikApplicationResources.ClusterFlows()
    {
      AuthorizationFlow = flows.AuthorizationImplicitConsent.Uuid,
      AuthenticationFlow = flows.AuthenticationFlow.Uuid,
      InvalidationFlow = flows.InvalidationFlow.Uuid,
    };
    var clusterBrand = new Brand(definition.Metadata.Name, new()
    {
      Domain = definition.Spec.Domain,
      BrandingLogo = definition.Spec.Icon ?? "",
      BrandingTitle = definition.Spec.Name,
      BrandingFavicon = definition.Spec.Favicon ?? "",
      // BrandingDefaultFlowBackground = "",
      FlowAuthentication = flows.AuthenticationFlow.Uuid,
      FlowInvalidation = flows.InvalidationFlow.Uuid,
      FlowUserSettings = flows.UserSettingsFlow.Uuid,
      // FlowDeviceCode = ,
      // FlowRecovery = ,
      // FlowUnenrollment = ,
    });
  }

  _ = new AuthentikApplicationResources(new()
  {
    Cluster = cluster,
    ClusterFlows = clusterFlows.ToImmutable()
  });

  // var plexSource = new SourcePlex("plex", new()
  // {
  //
  // });

  // TODO: Create users?

  // Export outputs here
  return new Dictionary<string, object?>
  {
    ["outputKey"] = "outputValue"
  };
});
