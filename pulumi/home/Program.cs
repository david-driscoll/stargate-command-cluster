using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using applications;
using Duende.IdentityModel.Client;
using home;
using k8s;
using k8s.KubeConfigModels;
using models;
using Pulumi;
using Pulumi.Automation;
using Rocket.Surgery.OnePasswordNativeUnofficial;
using Config = Pulumi.Config;

KubernetesJson.AddJsonOptions(options => { options.Converters.Add(new YamlMemberConverterFactory()); });



return await Deployment.RunAsync(async () =>
{
  // Kubernetes cluster;
  // {
  //
  //   static Kubernetes CreateClientAndProvider(string kubeConfig, string name, string? context = null)
  //   {
  //     using var stream = new MemoryStream(Encoding.ASCII.GetBytes(kubeConfig));
  //     var config = KubernetesClientConfiguration.LoadKubeConfig(stream);
  //     var clientConfig = KubernetesClientConfiguration.BuildConfigFromConfigObject(config);
  //     var client = new Kubernetes(clientConfig);
  //     if (context != null)
  //     {
  //       config.CurrentContext = context;
  //     }
  //
  //     return client;
  //   }
  //
  //   if (OperatingSystem.IsLinux())
  //   {
  //     cluster = new Kubernetes(await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(
  //       new MemoryStream(Encoding.UTF8.GetBytes(new Config("kubernetes").Require("kubeconfig")))));
  //   }
  //   else
  //   {
  //     var kubeConfig = File.ReadAllText(KubernetesClientConfiguration.KubeConfigDefaultLocation);
  //     cluster = CreateClientAndProvider(kubeConfig, "sgc", "admin@sgc");
  //   }
  // }
  //
  // await TailscaleUpdater.DoUpdate(new ()  { Id  = "kfpxs5w6zr3qetocx3wmdk3wxy", Title = "Tailscale Terraform OAuth Client", Vault = "Eris"  });

  var globals = new GlobalResources(new());

  var mainProxmox = GetAPICredential.Invoke(new()
    { Id = "fc4nrngoouan6dhuoopad3opcq", Title = "Proxmox ApiKey", Vault = "Eris", }, new InvokeOptions() { Provider = globals.OnePasswordProvider, Parent = globals});
  var alphaSiteProxmox = GetAPICredential.Invoke(new()
    { Id = "slm5zhqebezeubm6lgpqzn45mm", Title = "Alpha Site Proxmox ApiKey", Vault = "Eris", }, new InvokeOptions() { Provider = globals.OnePasswordProvider, Parent = globals });

  var twilightSparkle = new ProxmoxHost("twilight-sparkle", new()
  {
    Globals = globals,
    IsBackupServer = false,
    InternalIpAddress = "10.10.10.100",
    TailscaleIpAddress = "100.111.10.100",
    MacAddress = "58:47:ca:7b:a9:9d",
    Proxmox = alphaSiteProxmox,
  });

  var celestia = new ProxmoxHost("celestia", new()
  {
    Globals = globals,
    IsBackupServer = true,
    InternalIpAddress = "10.10.10.103",
    TailscaleIpAddress = "100.111.10.103",
    MacAddress = "c8:ff:bf:03:cc:4c",
    Proxmox = mainProxmox,
  });

  var luna = new ProxmoxHost("luna", new()
  {
    Globals = globals,
    IsBackupServer = true,
    InternalIpAddress = "10.10.10.104",
    TailscaleIpAddress = "100.111.10.104",
    MacAddress = "c8:ff:bf:03:c9:1e",
    Proxmox = mainProxmox,
  });

  var alphaSite = new ProxmoxHost("alpha-site", new()
  {
    Globals = globals,
    IsBackupServer = false,
    InternalIpAddress = "10.10.10.200",
    TailscaleIpAddress = "100.111.10.200",
    MacAddress = "e4:5f:01:90:36:22",
    Proxmox = alphaSiteProxmox,
    InstallTailscale = false
  });

  // Export outputs here
  return new Dictionary<string, object?>();
});
