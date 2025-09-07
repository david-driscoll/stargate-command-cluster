using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using applications;
using Duende.IdentityModel.Client;
using Dumpify;
using home;
using k8s;
using k8s.KubeConfigModels;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using models;
using Pulumi;
using Rocket.Surgery.OnePasswordNativeUnofficial;
using Tailscale.Client;
using Config = Pulumi.Config;

KubernetesJson.AddJsonOptions(options => { options.Converters.Add(new YamlMemberConverterFactory()); });

return await Deployment.RunAsync(async () =>
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

  await TailscaleUpdater.DoUpdate(new ()  { Id  = "kfpxs5w6zr3qetocx3wmdk3wxy", Title = "Tailscale Terraform OAuth Client", Vault = "Eris"  }, onePasswordProvider);

  var credentials = new ComponentResource("custom:home:Credentials", "credentials");

  var mainProxmox = GetAPICredential.Invoke(new()
    { Id = "fc4nrngoouan6dhuoopad3opcq", Title = "Proxmox ApiKey", Vault = "Eris", }, new InvokeOptions() { Provider = onePasswordProvider, Parent = credentials});
  var alphaSiteProxmox = GetAPICredential.Invoke(new()
    { Id = "slm5zhqebezeubm6lgpqzn45mm", Title = "Alpha Site Proxmox ApiKey", Vault = "Eris", }, new InvokeOptions() { Provider = onePasswordProvider, Parent = credentials });
  var cloudflare = GetAPICredential.Invoke(new()
    { Id = "7ntcze3fqqzun7huc7vyoirco4", Title = "Cloudflare (driscoll.tech)", Vault = "Eris" }, new InvokeOptions() { Provider = onePasswordProvider, Parent = credentials });

  var twilightSparkle = new ProxmoxHost("twilight-sparkle", new()
  {
    IsBackupServer = false,
    InternalIpAddress = "10.10.10.100",
    TailscaleIpAddress = "100.111.10.100",
    Proxmox = alphaSiteProxmox,
    Cloudflare = cloudflare,
  });

  var celestia = new ProxmoxHost("celestia", new()
  {
    IsBackupServer = true,
    InternalIpAddress = "10.10.10.103",
    TailscaleIpAddress = "100.111.10.103",
    Proxmox = mainProxmox,
    Cloudflare = cloudflare,
  });

  var luna = new ProxmoxHost("luna", new()
  {
    IsBackupServer = true,
    InternalIpAddress = "10.10.10.104",
    TailscaleIpAddress = "100.111.10.104",
    Proxmox = mainProxmox,
    Cloudflare = cloudflare,
  });

  var alphaSite = new ProxmoxHost("alpha-site", new()
  {
    IsBackupServer = false,
    InternalIpAddress = "10.10.10.200",
    TailscaleIpAddress = "100.111.10.200",
    Proxmox = alphaSiteProxmox,
    Cloudflare = cloudflare,
  });

  // Export outputs here
  return new Dictionary<string, object?>();
});

public static class TailscaleUpdater
{
  public static async Task DoUpdate(GetAPICredentialArgs credentialArgs, Rocket.Surgery.OnePasswordNativeUnofficial.Provider provider)
  {
    var credential = await GetAPICredential.InvokeAsync(credentialArgs, new InvokeOptions() { Provider = provider });
    var client = new HttpClient(new HttpClientHandler()
    {
      ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
    var tokenRequest = new FormUrlEncodedContent([
      new ("client_id", credential.Username),
      new ("client_secret", credential.Credential)
    ]);

    var tokenResponse = await client.PostAsync("https://api.tailscale.com/api/v2/oauth/token", tokenRequest);
    tokenResponse.EnsureSuccessStatusCode();

    var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
    // Parse the JSON response to get the access token
    var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tokenContent);
    var accessToken = tokenData["access_token"].ToString();

    var authProvider = new ApiKeyAuthenticationProvider($"Bearer {accessToken}", "Authorization", ApiKeyAuthenticationProvider.KeyLocation.Header);
    var adapter = new HttpClientRequestAdapter(authProvider, httpClient: client) { BaseUrl = "https://api.tailscale.com/api/v2/" };
    var tsClient = new TailscaleClient(adapter);
    (await tsClient.Tailnet["-"].Users.GetAsUsersGetResponseAsync()).Dump();
  }

}
