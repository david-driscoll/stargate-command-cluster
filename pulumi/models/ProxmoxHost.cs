using System;
using System.IO;
using Dumpify;
using Humanizer;
using Microsoft.Kiota.Abstractions;
using Pulumi;
using Pulumi.Cloudflare;
using Pulumi.ProxmoxVE;
using Pulumi.ProxmoxVE.Inputs;
using Pulumi.ProxmoxVE.Storage.Inputs;
using Pulumi.Tailscale;
using Rocket.Surgery.OnePasswordNativeUnofficial;
using Tailscale.Client.Device.Item.Ip;
using Tailscale.Client.Models;
using Tailscale.Client.Tailnet.Item.Keys;
using ProviderArgs = Pulumi.ProxmoxVE.ProviderArgs;

namespace models;

using RemoteConnectionArgs = Pulumi.Command.Remote.Inputs.ConnectionArgs;
using RemoteCommand = Pulumi.Command.Remote.Command;
using CopyToRemote = Pulumi.Command.Remote.CopyToRemote;

public class ProxmoxHost : ComponentResource
{
  public class Args : ResourceArgs
  {
    public required GlobalResources Globals { get; init; }
    public required Output<GetAPICredentialResult> Proxmox { get; init; }
    public required Input<string> InternalIpAddress { get; init; }
    public required Input<string> TailscaleIpAddress { get; init; }
    public required Input<string> MacAddress { get; init; }
    public required bool IsBackupServer { get; init; }
    public bool InstallTailscale { get; init; } = true;
  }

  public ProxmoxHost(string name, Args args, ComponentResourceOptions? options = null) : base(
    "home:proxmox:ProxmoxHost", name, options)
  {
    var cro = new CustomResourceOptions()
    {
      Parent = this
    };

    var apiCredential = args.Proxmox;

    var endpoint = apiCredential.Apply(z => z.Fields.TryGetValue("endpoint", out var v)
      ? v.Value
      : throw new InvalidOperationException("endpoint is required"));

    PveProvider = new Pulumi.ProxmoxVE.Provider($"{name}-pve-provider", new ProviderArgs()
    {
      RandomVmIds = true,
      RandomVmIdStart = 1000,
      RandomVmIdEnd = 1999,
      Endpoint = endpoint,
      ApiToken = Output.Format($"{apiCredential.Apply(z => z.Username)}={apiCredential.Apply(z => z.Credential)}"),
      Ssh = new ProviderSshArgs()
      {
        Username = "root",
        Password = args.Globals.Proxmox.Apply(z => z.Password!)
      }
    }, cro);

    LxcProvider = new Pulumi.Proxmox.Provider($"{name}-lxc-provider", new()
    {
      PmApiUrl = endpoint,
      PmApiTokenId = apiCredential.Apply(z => z.Username!),
      PmApiTokenSecret = apiCredential.Apply(z => z.Credential!),
    }, cro);

    var hostname = Output.Format($"{name}.host.driscoll.tech");
    var tailscaleHostname = Output.Format($"{name}.{args.Globals.TailscaleDomain}");

    // _ = new Pulumi.TailscaleNative.Device.RoutesConfig()

    CloudflareDns = new(name, new()
      {
        Name = hostname,
        ZoneId = args.Globals.CloudflareCredential.Apply(z => z.Fields["zoneId"].Value),
        Content = args.InternalIpAddress,
        Type = "A",
        Ttl = 1,
      },
      CustomResourceOptions.Merge(cro,
        new() { Provider = args.Globals.CloudflareProvider, DeleteBeforeReplace = true }));

    UnifiDns = new(name, new()
    {
      Name = hostname,
      Type = "A",
      Record = args.InternalIpAddress,
      Ttl = 0
    }, CustomResourceOptions.Merge(cro, new() { Provider = args.Globals.UnifiProvider, DeleteBeforeReplace = true }));

    var connection = new RemoteConnectionArgs()
    {
      Host = args.InternalIpAddress,
      User = args.Globals.Proxmox.Apply(z => z.Username!),
      Password = args.Globals.Proxmox.Apply(z => z.Password!)
    };

    if (args.InstallTailscale)
    {
      var installJq = new RemoteCommand($"{name}-install-jq", new()
      {
        Connection = connection,
        Create = "apt-get install -y jq"
      }, cro);

      var installTailscale = new RemoteCommand($"{name}-install-tailscale", new()
      {
        Connection = connection,
        Create = "curl -fsSL https://tailscale.com/install.sh | sh"
      }, cro);

      var tailscaleSet = new RemoteCommand($"{name}-tailscale-set", new()
      {
        Connection = connection,
        Create = Output.Format($"TS_AUTHKEY={args.Globals.TailscaleAuthKey.Key} tailscale set --hostname {name} --accept-dns --accept-routes --auto-update --advertise-exit-node --ssh=true")
      });

      var tailscaleCron = new CopyToRemote($"{name}-tailscale-cron", new()
      {
        Connection = connection,
        RemotePath = "/etc/cron.weekly/tailscale",
        Source = new FileAsset(args.IsBackupServer ? "scripts/tailscale-pbs.sh" : "scripts/tailscale.sh"),
      }, CustomResourceOptions.Merge(cro, new() { DependsOn = [installTailscale, tailscaleSet, installJq] }));

      var tailscaleSetCert = new RemoteCommand($"{name}-install-set", new()
      {
        Connection = connection,
        Create = "chmod 755 /etc/cron.weekly/tailscale && /etc/cron.weekly/tailscale"
      }, CustomResourceOptions.Merge(cro, new() { DependsOn = [tailscaleCron] }));
    }

    var device = GetDevice.Invoke(new()
    {
      Hostname = name,
    }, new () {Provider = args.Globals.TailscaleProvider, Parent = this});

    if (args.InstallTailscale)
    {
      _ = new DeviceTags($"{name}-tags", new()
      {
        Tags = ["tag:proxmox", "tag:exit-node"],
        DeviceId = device.Apply(z => z.Id),
      }, new () {Provider = args.Globals.TailscaleProvider, Parent = this, RetainOnDelete = true });

      _ = new DeviceKey($"{name}-key", new()
      {
        KeyExpiryDisabled = true,
        DeviceId = device.Apply(z => z.Id),
      }, new () {Provider = args.Globals.TailscaleProvider, Parent = this});
    }

    Device = Output.Tuple(device, args.Globals.TailscaleClient, args.TailscaleIpAddress.ToOutput())
      .Apply(async z =>
      {
        var (device, client, ip) = z;
        await client.Device[device.Id].Ip.PostAsync(new IpPostRequestBody()
        {
          Ipv4 = ip
        });
        return device;
      });


    // _ = new Hosts($"{name}-hosts", new ()
    // {
    //   NodeName = name,
    //   Entry =
    //   [
    //     new HostsEntryArgs() { Address = "127.0.0.1", Hostnames = ["localhost.localdomain", "localhost"] },
    //     new HostsEntryArgs() { Address = args.InternalIpAddress, Hostnames = [hostname, $"{name}"] },
    //     new HostsEntryArgs() { Address = args.TailscaleIpAddress, Hostnames = [tailscaleHostname] },
    //     new HostsEntryArgs() { Address = "::1", Hostnames = ["ip6-localhost", "ip6-loopback"] },
    //     new HostsEntryArgs() { Address = "fe00::0", Hostnames = ["ip6-localnet"] },
    //     new HostsEntryArgs() { Address = "ff00::0", Hostnames = ["ip6-mcastprefix"] },
    //     new HostsEntryArgs() { Address = "ff02::1", Hostnames = ["ip6-allnodes"] },
    //     new HostsEntryArgs() { Address = "ff02::2", Hostnames = ["ip6-allrouters"] },
    //     new HostsEntryArgs() { Address = "ff02::3", Hostnames = ["ip6-allhosts"] },
    //   ]
    // }, CustomResourceOptions.Merge(cro, new () { Provider = PveProvider }));

    // /etc/.pve-ignore.resolv.conf
  }


  public Output<GetDeviceResult> Device { get; set; }

  public Pulumi.Unifi.DnsRecord UnifiDns { get; set; }

  public Pulumi.Cloudflare.DnsRecord CloudflareDns { get; set; }

  public Pulumi.ProxmoxVE.Provider PveProvider { get; }
  public Pulumi.Proxmox.Provider LxcProvider { get; }
}
