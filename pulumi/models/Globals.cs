using System;
using Pulumi;
using Pulumi.Tailscale;
using Rocket.Surgery.OnePasswordNativeUnofficial;
using Tailscale.Client;

namespace models;

using CloudflareProvider = Pulumi.Cloudflare.Provider;
using OnePasswordProvider = Rocket.Surgery.OnePasswordNativeUnofficial.Provider;
using TailscaleProvider = Pulumi.Tailscale.Provider;
using UnifiProvider = Pulumi.Unifi.Provider;

public class GlobalResources : ComponentResource
{
  public class Args : ResourceArgs;

  public GlobalResources(Args args, ComponentResourceOptions? options = null) : base(
    "custom:home:resources", "globals", args, options)
  {
    CustomResourceOptions cro = new() { Parent = this };
    OnePasswordProvider = new("onepassword", new()
    {
      Vault = Environment.GetEnvironmentVariable("CONNECT_VAULT") ??
              throw new InvalidOperationException("CONNECT_VAULT is not set"),
      ConnectHost = Environment.GetEnvironmentVariable("CONNECT_HOST") ??
                    throw new InvalidOperationException("CONNECT_HOST is not set"),
      ConnectToken = Environment.GetEnvironmentVariable("CONNECT_TOKEN") ??
                     throw new InvalidOperationException("CONNECT_TOKEN is not set"),
    }, cro);
    OnePasswordInvokeOptions = new() { Provider = OnePasswordProvider, Parent = this };

    CloudflareCredential = GetAPICredential.Invoke(new()
    {
      Title = "Cloudflare (driscoll.tech)",
      Id = "7ntcze3fqqzun7huc7vyoirco4",
      Vault = "Eris"
    }, OnePasswordInvokeOptions);

    CloudflareProvider = new("cloudflare", new()
    {
      ApiToken = CloudflareCredential.Apply(z => z.Credential!),
    }, cro);

    UnifiCredential = GetAPICredential.Invoke(new()
    {
      Title = "Unifi Api Key Eris Cluster",
      Id = "nxhctyryy6b7hauxfysmzev6uu",
      Vault = "Eris"
    }, OnePasswordInvokeOptions);

    UnifiProvider = new("unifi", new()
    {
      ApiUrl = UnifiCredential.Apply(z => z.Hostname!),
      ApiKey = UnifiCredential.Apply(z => z.Credential!),
    }, cro);
    Proxmox = GetLogin.Invoke(new()
    {
      Title = "Proxmox",
      Id = "cixmyifwao3ynqtay53rh7b3ny",
      Vault = "Eris"
    }, OnePasswordInvokeOptions);

    TailscaleCredential = GetAPICredential.Invoke(new()
    {
      Title = "Tailscale Terraform OAuth Client",
      Id = "kfpxs5w6zr3qetocx3wmdk3wxy",
      Vault = "Eris"
    }, OnePasswordInvokeOptions);

    TailscaleProvider = new("tailscale", new()
    {
      OauthClientId = TailscaleCredential.Apply(z => z.Username!),
      OauthClientSecret = TailscaleCredential.Apply(z => z.Credential!),
    });

    TailscaleDomain = TailscaleCredential.Apply(z => z.Hostname!);

    TailscaleClient = TailscaleClientFactory.CreateClient(TailscaleCredential);

    TailscaleAuthKey = new("tailnetkey", new()
    {
      Reusable = true,
      Preauthorized = true,
      Ephemeral = true,
      // Expiry = Convert.ToInt32(TimeSpan.FromHours(1).TotalSeconds),
      RecreateIfInvalid = "always",
      Tags = ["tag:proxmox", "tag:apps"],
      Description = "Proxmox Management Key",
    }, CustomResourceOptions.Merge(cro, new() { Provider = TailscaleProvider }));
  }

  public InvokeOptions OnePasswordInvokeOptions { get; set; }
  public Output<string> TailscaleDomain { get; set; }
  public Output<TailscaleClient> TailscaleClient { get; set; }
  public Output<GetLoginResult> Proxmox { get; set; }
  public OnePasswordProvider OnePasswordProvider { get; }
  public Output<GetAPICredentialResult> CloudflareCredential { get; }
  public Output<GetAPICredentialResult> TailscaleCredential { get; }
  public CloudflareProvider CloudflareProvider { get; }
  public Output<GetAPICredentialResult> UnifiCredential { get; }
  public UnifiProvider UnifiProvider { get; }
  public TailscaleProvider TailscaleProvider { get; }
  public TailnetKey TailscaleAuthKey { get; }
}
