using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
using Rocket.Surgery.OnePasswordNativeUnofficial;
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
  var onePasswordProvider = new Rocket.Surgery.OnePasswordNativeUnofficial.Provider("onepassword", new()
  {
    Vault = Environment.GetEnvironmentVariable("CONNECT_VAULT") ?? throw new InvalidOperationException("CONNECT_VAULT is not set"),
    ConnectHost = Environment.GetEnvironmentVariable("CONNECT_HOST") ?? throw new InvalidOperationException("CONNECT_HOST is not set"),
    ConnectToken = Environment.GetEnvironmentVariable("CONNECT_TOKEN") ?? throw new InvalidOperationException("CONNECT_TOKEN is not set"),
  });
  var kumaGroups = new KumaGroups();

  _ = new KumaUptimeResources(new()
  {
    Cluster = cluster,
    Groups = kumaGroups,
  });

  var clusters =
    (await cluster.ListClusterCustomObjectAsync<ClusterDefinitionList>("driscoll.dev", "v1", "clusterdefinitions")).Items.ToImmutableArray();
  var clusterFlows = ImmutableDictionary.CreateBuilder<string, AuthentikApplicationResources.ClusterFlows>();


  var flows = Flows2.CreateFlows();
  foreach (var definition in clusters)
  {
    var definitionFlows = Flows2.CreateClusterFlows(definition, onePasswordProvider);
    clusterFlows[definition.Metadata.Name] = new AuthentikApplicationResources.ClusterFlows()
    {
      AuthorizationFlow = flows.ImplicitConsentFlow.Uuid,
      AuthenticationFlow = definitionFlows.AuthenticationFlow.Uuid,
      InvalidationFlow = flows.LogoutFlow.Uuid,
    };
    var clusterBrand = new Brand(definition.Metadata.Name, new()
    {
      Domain = new Uri(definition.Spec.Domain).Host,
      BrandingLogo = definition.Spec.Icon ?? "",
      BrandingTitle = definition.Spec.Name,
      BrandingFavicon = definition.Spec.Favicon ?? "",
      // BrandingDefaultFlowBackground = "",
      FlowAuthentication = definitionFlows.AuthenticationFlow.Uuid,
      FlowInvalidation = flows.LogoutFlow.Uuid,
      FlowUserSettings = flows.UserSettingsFlow.Uuid,
      // FlowDeviceCode = ,
      // FlowRecovery = ,
      // FlowUnenrollment = ,
    });
  }

  _ = new AuthentikApplicationResources(new()
  {
    Cluster = cluster,
    ClusterInfo = clusters.ToImmutableDictionary(z => z.Metadata.Name, z => z),
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
