using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using applications;
using applications.KumaResources;
using Dumpify;
using k8s;
using k8s.KubeConfigModels;
using models;
using Pulumi;
using Rocket.Surgery.OnePasswordNativeUnofficial;
using Config = Pulumi.Config;

KubernetesJson.AddJsonOptions(options => { options.Converters.Add(new YamlMemberConverterFactory()); });

return await Deployment.RunAsync(async () =>
{
  Kubernetes cluster;
  {
    var onePasswordProvider = new Rocket.Surgery.OnePasswordNativeUnofficial.Provider("onepassword", new()
    {
      Vault = Environment.GetEnvironmentVariable("CONNECT_VAULT") ??
              throw new InvalidOperationException("CONNECT_VAULT is not set"),
      ConnectHost = Environment.GetEnvironmentVariable("CONNECT_HOST") ??
                    throw new InvalidOperationException("CONNECT_HOST is not set"),
      ConnectToken = Environment.GetEnvironmentVariable("CONNECT_TOKEN") ??
                     throw new InvalidOperationException("CONNECT_TOKEN is not set"),
    });

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

    try
    {
      await PopulateCluster.PopulateClusters(cluster);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error populating cluster: {ex.Message}");
      ex.Dump();
    }

    // try
    // {
    //   await PopulateHomarr.Populate(cluster, await GetAPICredential.InvokeAsync(
    //     new() { Vault = "Eris", Title = "Homarr Api Key", Id = "dfssrapsyraazvn574sab6ozsq" },
    //     new() { Provider = onePasswordProvider }
    //   ));
    // }
    // catch (Exception ex)
    // {
    //   Console.WriteLine($"Error populating homarr: {ex.Message}");
    //   ex.Dump();
    // }
  }

  var kumaGroups = new KumaGroups();

  _ = new KumaUptimeResources(new()
  {
    Cluster = cluster,
    Groups = kumaGroups,
  });

  // Export outputs here
  return new Dictionary<string, object?>();
});
