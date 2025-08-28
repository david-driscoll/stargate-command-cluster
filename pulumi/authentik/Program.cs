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
using Rocket.Surgery.OnePasswordNativeUnofficial;
using Config = Pulumi.Config;
using Pulumi.N8n;
using Pulumi.N8n.Inputs;
using UserArgs = Pulumi.N8n.UserArgs;

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
      cluster = new Kubernetes(await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(
        new MemoryStream(Encoding.UTF8.GetBytes(new Config("kubernetes").Require("kubeconfig")))));
    }
    else
    {
      var kubeConfig = File.ReadAllText(KubernetesClientConfiguration.KubeConfigDefaultLocation);
      cluster = CreateClientAndProvider(kubeConfig, "sgc", "admin@sgc");
    }
  }

  var groups = new AuthentikGroups();
  var onePasswordProvider = new Rocket.Surgery.OnePasswordNativeUnofficial.Provider("onepassword", new()
  {
    Vault = Environment.GetEnvironmentVariable("CONNECT_VAULT") ??
            throw new InvalidOperationException("CONNECT_VAULT is not set"),
    ConnectHost = Environment.GetEnvironmentVariable("CONNECT_HOST") ??
                  throw new InvalidOperationException("CONNECT_HOST is not set"),
    ConnectToken = Environment.GetEnvironmentVariable("CONNECT_TOKEN") ??
                   throw new InvalidOperationException("CONNECT_TOKEN is not set"),
  });

  var flows = Flows2.CreateFlows(onePasswordProvider);
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

  var appResources = new AuthentikApplicationResources(new()
  {
    OnePasswordProvider = onePasswordProvider,
    Groups = groups,
    Cluster = cluster,
    ClusterInfo = clusters.ToImmutableDictionary(z => z.Metadata.Name, z => z),
    ClusterFlows = clusterFlows,
    PropertyMappings = Flows2.PropertyMappings,
  });

  // var n8nToken =
  //   GetAPICredential.Invoke(
  //     new GetAPICredentialInvokeArgs() { Id = "d7it3mahq5itdb32ezbtmelube", Title = "N8N Api Key", Vault = "Eris" },
  //     new InvokeOptions() { Provider = onePasswordProvider });
  // var n8nProvider = new Pulumi.N8n.Provider("n8n", new Pulumi.N8n.ProviderArgs()
  // {
  //   ApiKey = n8nToken.Apply(z => z.Credential),
  //   BaseUrl = Output.Format($"https://{n8nToken.Apply(z => z.Hostname)}"),
  // });
  //
  // GetUsers.Invoke(new() { IsActive = true })
  //   .Apply(z => z.Users.Select(user => new Pulumi.N8n.User(user.Name, new UserArgs()
  //     {
  //       Email = user.Email,
  //       FirstName = user.Name,
  //     }, new() { Parent = appResources, Provider = n8nProvider })
  //   ));
  // ;

  // Export outputs here
  return new Dictionary<string, object?>();
});
