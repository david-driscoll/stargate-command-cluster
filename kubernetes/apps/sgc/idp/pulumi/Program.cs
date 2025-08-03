using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dumpify;
using Humanizer;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Pulumi;
using Pulumi.Authentik;
using StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;
using Rocket.Surgery.OnePasswordNativeUnofficial;
using KubernetesProvider = Pulumi.Kubernetes.Provider;
using Pulumi.Uptimekuma;

//const string rootDomain = "${ROOT_DOMAIN}";
const string rootDomain = "driscoll.dev"; // For local testing, change this to your actual domain or a test domain.
return await Deployment.RunAsync(async () =>
{
  // Add your resources here
  // e.g. var resource = new Resource("name", new ResourceArgs { });
  // var sgcConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile("/config/${APP}-${CLUSTER_CNAME}-kubeconfig");
  // var equestriaConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile("/config/${APP}-equestria-kubeconfig");
  var sgcConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: "admin@sgc");
  var equestriaConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: "admin@equestria");
  var secrets = new Pulumi.Config();

  var groups = new AuthentikGroups();

  var sgcClient = new Kubernetes(sgcConfig);
  var equestriaClient = new Kubernetes(equestriaConfig);

  var tailscaleSource = new SourceOauth("tailscale", new()
  {
    Name = "Tailscale",
    Slug = "tailscale",
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

  var sgcResources = new ClusterApplicationResources("sgc", new()
  {
    ClusterName = "sgc",
    ClusterTitle = "Stargate Command",
    UptimeCluster = sgcClient,
    RemoteCluster = sgcClient,
    ServiceConnection = new ServiceConnectionKubernetes("sgc", new()
    {
      Name = "Stargate Command",
      VerifySsl = true,
      Local = true,
    })
  });
  var equestriaResources = new ClusterApplicationResources("equestria", new()
  {
    ClusterName = "sgc",
    ClusterTitle = "Stargate Command",
    UptimeCluster = sgcClient,
    RemoteCluster = equestriaClient,
    ServiceConnection = new ServiceConnectionKubernetes("equestria", new()
    {
      Name = "Equestria",
      VerifySsl = true,
      Kubeconfig = LoadKubeConfigFromPulumiConfig(secrets, "cluster_equestria")
        .Apply(kubeconfig => kubeconfig),
    })
  });

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
