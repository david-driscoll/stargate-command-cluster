using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using applications;
using applications.KumaResources;
using Dumpify;
using k8s;
using models;
using Pulumi;

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

  var kumaGroups = new KumaGroups();

  _ = new KumaUptimeResources(new()
  {
    Cluster = cluster,
    Groups = kumaGroups,
  });

  // Export outputs here
  return new Dictionary<string, object?>();
});
