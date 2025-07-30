using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dumpify;
using Humanizer;
using k8s;
using k8s.KubeConfigModels;
using k8s.Models;
using Pulumi;
using Pulumi.Authentik;
using Pulumi.Kubernetes;
using Pulumi.Kubernetes.ApiExtensions.V1;

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

  var defaultSourceAuthentication = GetFlow.Invoke(new() { Slug = "default-authentication-flow" });
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
  var sgcConnection = new ServiceConnectionKubernetes("sgc", new()
  {
    Name = "Stargate Command",
    VerifySsl = true,
    Local = true,
  });
  var equestriaConnection = new ServiceConnectionKubernetes("equestria", new()
  {
    Name = "Equestria",
    VerifySsl = true,
    Kubeconfig = LoadKubeConfigFromPulumiConfig(secrets, "cluster_equestria").Apply(kubeconfig => kubeconfig.Dump("K8SConfigurationSerialized")),
  });

  await CreateApplications("sgc", "Stargate Command", sgcConnection, sgcClient);
  await CreateApplications("equestria", "Equestria", equestriaConnection, equestriaClient);
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

static async Task CreateApplications(string clusterName, string clusterTitle, ServiceConnectionKubernetes serviceConnection, Kubernetes cluster)
{


  clusterName.Dump();
  var providers = new List<CustomResource>();
  await foreach (var app in GetApplications(cluster))
  {
    var slug = app.Spec.Slug ?? $"{clusterName}-{app.Metadata.Name.Dasherize()}";
    CustomResource provider = app.Spec.Provider.ToLowerInvariant() switch
    {
      "oauth2" => new ProviderOauth2(app.Metadata.Name, new()
      {
        Name = Output.Format($"Provider for {app.Metadata.Name} ({clusterTitle})"),
        ClientId = app.Spec.Config["clientId"],
        ClientSecret = app.Spec.Config["clientSecret"],
        ClientType = "confidential",
        // todo
      }),
      "forward-auth" => new ProviderProxy(app.Metadata.Name, new()
      {
        Name = Output.Format($"Provider for {app.Metadata.Name} ({clusterTitle})"),
        Mode = "forward_single",
        ExternalHost = app.Spec.Url,
        AuthorizationFlow = Flows.DefaultAuthenticationFlow.Apply(f => f.Id),
        InvalidationFlow = Flows.DefaultInvalidationFlow.Apply(f => f.Id),
      }),
      "proxy" => new ProviderProxy(app.Metadata.Name, new()
      {
        Name = Output.Format($"Provider for {app.Metadata.Name} ({clusterTitle})"),
        Mode = "proxy",
        ExternalHost = app.Spec.Url,
        AuthorizationFlow = Flows.DefaultAuthenticationFlow.Apply(f => f.Id),
        InvalidationFlow = Flows.DefaultInvalidationFlow.Apply(f => f.Id),
      }),
      _ => throw new ArgumentException($"Unknown provider: {app.Spec.Provider}")
    };
    providers.Add(provider);

    var application = new Application(app.Metadata.Name, new()
    {
      // ApplicationId = ,
      ProtocolProvider = provider.Id.Apply(id => double.Parse(id)),
      Name = app.Metadata.Name,
      Slug = app.Spec.Slug ?? app.Metadata.Name.ToLowerInvariant().Dasherize(),
      Group = app.Spec.Category,
      MetaIcon = app.Spec.Icon,
      MetaPublisher = clusterTitle,
      MetaDescription = app.Spec.Description ?? "",
      MetaLaunchUrl = app.Spec.Url,
      // PolicyEngineMode = "any",
      // OpenInNewTab = true,
    });
  }
  if (!providers.Any()) return;
  var outpost = new Outpost($"authentik-{clusterName}-outpost", new()
  {
    ServiceConnection = serviceConnection.Id,
    Type = "proxy",
    Name = Output.Format($"Outpost for {clusterTitle}"),
    Config = Output.JsonSerialize(Output.Create(new
    {
      object_naming_template = $"authentik-outpost-{clusterName}",
      kubernetes_replicas = 2,
      kubernetes_namespace = clusterName,
      kubernetes_ingress_class_name = "internal"
    })),
    ProtocolProviders = [.. providers.Select(z => z.Id.Apply(id => double.Parse(id)))]
  });
}

static void DumpNames(string title, IEnumerable<AuthentikApplicationResource> resources)
{
  resources.Select(MapName).Dump(title);
}
static string MapName(AuthentikApplicationResource resource) => $"{resource.Metadata.Namespace()}@{resource.Metadata.Name}";
static async IAsyncEnumerable<AuthentikApplicationResource> GetApplications(Kubernetes cluster)
{
  var namespaces = await cluster.ListNamespaceAsync();

  foreach (var ns in namespaces.Items)
  {
    var result = await cluster.CustomObjects.ListNamespacedCustomObjectAsync<AuthentikApplicationResourceList>("authentik.driscoll.dev", "v1", ns.Metadata.Name, "authentikapplications");
    await foreach (var item in result.Items.ToAsyncEnumerable())
    {
      yield return item;
    }
  }
}

static Func<AuthentikApplicationResource, AuthentikApplicationResource> MapRemoteEntity(string cluster) => resource =>
{
  resource.Metadata.Name = $"{cluster}-{resource.Metadata.Namespace()}-{resource.Metadata.Name}";
  resource.Metadata.SetNamespace("observability");
  resource.Metadata.Labels ??= new Dictionary<string, string>();
  resource.Metadata.Labels[$"{rootDomain}.cluster"] = cluster;
  resource.Metadata.ResourceVersion = null;
  return resource;
};
public class AuthentikApplicationResource : KubernetesObject, IMetadata<V1ObjectMeta>
{
  [JsonPropertyName("metadata")]
  public V1ObjectMeta Metadata { get; set; }

  [JsonPropertyName("spec")]
  public AuthentikApplicationSpec Spec { get; set; }

  [JsonPropertyName("status")]
  public AuthentikApplicationStatus Status { get; set; }
}

public class AuthentikApplicationStatus
{
  public string Id { get; set; }
}

public class AuthentikApplicationSpec
{
  public string Provider { get; set; } // e.g. "oauth2", "ldap", "forward-auth", "proxy"
  public string? Slug { get; set; }
  public string Url { get; set; }
  public string Icon { get; set; }
  public string? Description { get; set; }
  public string Category { get; set; }
  public Dictionary<string, string> Config { get; set; }
}

public class AuthentikApplicationResourceList : KubernetesObject
{
  public V1ListMeta Metadata { get; set; }
  public List<AuthentikApplicationResource> Items { get; set; }
}

