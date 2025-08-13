using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using applications;
using Dumpify;
using k8s;
using Microsoft.Extensions.DependencyInjection;
using Models.ApplicationDefinition;
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

  _ = new AuthentikGroups();
  var kumaGroups = new KumaGroups();
  _ = new AuthentikApplicationResources(new()
  {
    Cluster = cluster,
    AuthorizationFlow = Defaults.Flows.ProviderAuthorizationImplicitConsent.Apply(z => z.Id),
    InvalidationFlow = Defaults.Flows.InvalidationFlow.Apply(z => z.Id),
    AuthenticationFlow = Defaults.Flows.AuthenticationFlow.Apply(z => z.Id),
  });

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

    var defaultAuthenticationFlow = Defaults.Flows.AuthenticationFlow;
    var defaultInvalidationFlow = Defaults.Flows.InvalidationFlow;
    var defaultUserSettingsFlow = Defaults.Flows.UserSettingsFlow;
  var clusters =
    (await cluster.ListClusterCustomObjectAsync<ClusterDefinitionList>("driscoll.dev", "v1",
      "clusterdefinitions")).Items.ToImmutableArray();
  foreach (var branding in clusters)
  {
    var clusterBrand = new Brand(branding.Metadata.Name, new()
    {
      Domain = branding.Spec.Domain,
      BrandingLogo = branding.Spec.Icon ?? "",
      BrandingTitle = branding.Spec.Name,
      BrandingFavicon = branding.Spec.Favicon ?? "",
      BrandingDefaultFlowBackground = "",
      FlowAuthentication = Defaults.Flows.AuthenticationFlow.Apply(z => z.Id),
      FlowInvalidation = Defaults.Flows.InvalidationFlow.Apply(z => z.Id),
      FlowUserSettings = Defaults.Flows.UserSettingsFlow.Apply(z => z.Id),
      // FlowDeviceCode = ,
      // FlowRecovery = ,
      // FlowUnenrollment = ,
    });


    var authenticationFlow = new Flow($"{branding.Metadata.Name}-authentication",new()
    {
      Authentication = ,
      
    })
  }

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
