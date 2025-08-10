using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using applications;
using k8s;
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
  // Add your resources here
  // e.g. var resource = new Resource("name", new ResourceArgs { });
  static ClientContext CreateClientFromConfig(Pulumi.Config config,
    string key, string? context = null)
  {
    var kubeConfig = config.RequireSecret(key);
    return kubeConfig == null ? throw new ArgumentException($"Kubeconfig for {key} not found in Pulumi config.") : CreateClientAndProvider(kubeConfig, key.Replace(":", "_"), context);
  }

  static ClientContext CreateClientAndProvider(Input<string> kubeConfig, string name, string? context = null)
  {
    return kubeConfig.Apply(kubeConfig =>
    {
      using var stream = new MemoryStream(Encoding.ASCII.GetBytes(kubeConfig));
      var config = KubernetesClientConfiguration.LoadKubeConfig(stream);
      var clientConfig = KubernetesClientConfiguration.BuildConfigFromConfigObject(config);
      var client = new Kubernetes(clientConfig);
      ProviderArgs args = new()
      {
        KubeConfig = kubeConfig,
        Context = context ?? config.CurrentContext,
      };
      var provider = new KubernetesProvider(name, args);
      if (context != null)
      {
        config.CurrentContext = context;
      }

      var json = KubernetesJson.Serialize(config);
      return (client, provider, json);
    });
  }
  ClientContext sgc, equestria;
  string postfix;
  if (OperatingSystem.IsLinux())
  {
    var secrets = new Pulumi.Config();
    postfix = "";
    sgc = CreateClientFromConfig(secrets, "cluster_sgc");
    equestria = CreateClientFromConfig(secrets, "cluster_equestria");
  }
  else
  {
    postfix = "-test";
    var kubeConfig = File.ReadAllText(KubernetesClientConfiguration.KubeConfigDefaultLocation);
    sgc = CreateClientAndProvider(kubeConfig, "sgc", "admin@sgc");
    equestria = CreateClientAndProvider(kubeConfig, "equestria", "admin@equestria");
  }

  _ = new AuthentikGroups();
  CreateApplicationResources(sgc, sgc, "sgc", "Stargate Command");
  CreateApplicationResources(sgc, equestria, "equestria", "Equestria");

  // tailscale dns needs to be fixed
  var tailscaleSource = new SourceOauth("tailscale", new()
  {
    Name = applications.Mappings.PostfixTitle("Tailscale"),
    Slug = applications.Mappings.PostfixName("tailscale"),
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

  static ClusterApplicationResources CreateApplicationResources(ClientContext uptimeCluster, ClientContext remoteCluster, string clusterName, string clusterTitle)
  {
    return new ClusterApplicationResources(clusterName, new()
    {
      ClusterName = applications.Mappings.PostfixName(clusterName),
      ClusterTitle = applications.Mappings.PostfixTitle(clusterTitle),
      UptimeCluster = uptimeCluster.Apply(z => (z.Client, z.Provider)),
      RemoteCluster = remoteCluster.Apply(z => (z.Client, z.Provider)),
      InvalidationFlow = Defaults.Flows.InvalidationFlow.Apply(z => z.Id),
      AuthorizationFlow = Defaults.Flows.ProviderAuthorizationImplicitConsent.Apply(z => z.Id),
      // AuthenticationFlow = ,
      ServiceConnection = new ServiceConnectionKubernetes(applications.Mappings.PostfixName(clusterName), new()
      {
        Name = applications.Mappings.PostfixTitle(clusterTitle),
        VerifySsl = true,
        Local = true,
      })
    });
  }

  // TODO: Create users?

  // Export outputs here
  return new Dictionary<string, object?>
  {
    ["outputKey"] = "outputValue"
  };
});

static Output<string> LoadKubeConfigFromPulumiConfig(Pulumi.Config config, string key)
{
  return config.GetSecret(key)!;
}
