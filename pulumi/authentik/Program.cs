using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using authentik.AuthentikResources;
using k8s;
using models;
using models.Applications;
using Pulumi;
using Pulumi.Authentik;
using Config = Pulumi.Config;

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
      cluster = new Kubernetes(await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(new Config("kubernetes").Require("kubeconfig")))));
    }
    else
    {
      var kubeConfig = File.ReadAllText(KubernetesClientConfiguration.KubeConfigDefaultLocation);
      cluster = CreateClientAndProvider(kubeConfig, "sgc", "admin@sgc");
    }
  }

  var groups = new AuthentikGroups();
  var globals = new GlobalResources();

  var flows = Flows2.CreateFlows(Globals.OnePasswordProvider);
  var clusterFlows = new AuthentikApplicationResources.ClusterFlows()
  {
    AuthorizationFlow = flows.ImplicitConsentFlow.Uuid,
    AuthenticationFlow = flows.AuthenticationFlow.Uuid,
    InvalidationFlow = flows.LogoutFlow.Uuid,
  };
  var clusters = await Mappings.GetClusters(cluster).ToArrayAsync();
  foreach (var definition in clusters)
  {
    var clusterBrand = new Brand(definition.Metadata.Name, new()
    {
      Domain = new Uri(definition.Spec.Domain).Host,
      BrandingLogo = definition.Spec.Icon ?? "",
      BrandingTitle = definition.Spec.Name,
      BrandingFavicon = definition.Spec.Favicon ?? "",
      BrandingDefaultFlowBackground = definition.Spec.Background ?? "/static/dist/assets/images/flow_background.jpg",
      FlowAuthentication = flows.AuthenticationFlow.Uuid,
      FlowInvalidation = flows.LogoutFlow.Uuid,
      FlowUserSettings = flows.UserSettingsFlow.Uuid,
      // FlowDeviceCode = ,
      // FlowRecovery = ,
      // FlowUnenrollment = ,
    });
  }

  _ = new AuthentikApplicationResources(new()
  {
    Groups = groups,
    Cluster = cluster,
    ClusterInfo = clusters.ToImmutableDictionary(z => z.Metadata.Name, z => z),
    ClusterFlows = clusterFlows,
    PropertyMappings = Flows2.PropertyMappings,
  });

  // Export outputs here
  return new Dictionary<string, object?>();
});
