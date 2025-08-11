using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using applications;
using k8s;
using Models.ApplicationDefinition;
using Pulumi;
using Pulumi.Authentik;
using ClientContext = Pulumi.Output<(k8s.Kubernetes Client, Pulumi.Kubernetes.Provider Provider, string KubeConfig)>;
using KubernetesProvider = Pulumi.Kubernetes.Provider;
using ProviderArgs = Pulumi.Kubernetes.ProviderArgs;

KubernetesJson.AddJsonOptions(options =>
{
  options.Converters.Add(new YamlMemberConverterFactory());
});


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

    await PopulateCluster.PopulateClusters(cluster);
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

  // tailscale dns needs to be fixed
  var tailscaleSource = new SourceOauth("tailscale", new()
  {
    Name = Mappings.PostfixTitle("Tailscale"),
    Slug = Mappings.PostfixName("tailscale"),
    ProviderType = "openidconnect",
    Enabled = true,
    AuthenticationFlow = null,
    EnrollmentFlow = null,

    OidcWellKnownUrl = "https://idp.opossum-yo.ts.net/.well-known/openid-configuration",
    ConsumerKey = "unused",
    ConsumerSecret = "unused",
    UserMatchingMode = "email_link",
    GroupMatchingMode = "name_link",
  });

  // TODO: Create users?

  // Export outputs here
  return new Dictionary<string, object?>
  {
    ["outputKey"] = "outputValue"
  };
});

static Output<string> LoadKubeConfigFromPulumiConfig(Config config, string key)
{
  return config.GetSecret(key)!;
}
