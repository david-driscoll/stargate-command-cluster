using System;
using Pulumi;
using Pulumi.ProxmoxVE;
using Pulumi.ProxmoxVE.Inputs;
using Pulumi.ProxmoxVE.Storage.Inputs;
using Rocket.Surgery.OnePasswordNativeUnofficial;
using ProviderArgs = Pulumi.ProxmoxVE.ProviderArgs;

namespace home;

public class ProxmoxHost : ComponentResource
{
  public class Args : ResourceArgs
  {
    public required Output<GetAPICredentialResult> Proxmox { get; set; }
    public required Input<string> InternalIpAddress { get; set; }
    public required Input<string> TailscaleIpAddress { get; set; }
    public required bool IsBackupServer { get; set; }
    public required Output<GetAPICredentialResult> Cloudflare { get; set; }
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
        PrivateKey = ""
      }
    }, cro);

    LxcProvider = new Pulumi.Proxmox.Provider($"{name}-lxc-provider", new()
    {
      PmApiUrl = endpoint,
      PmApiTokenId = apiCredential.Apply(z => z.Username),
      PmApiTokenSecret = apiCredential.Apply(z => z.Credential),
    }, cro);

    // _ = new Pulumi.TailscaleNative.Device.RoutesConfig()

    _ = new Hosts($"{name}-hosts", new HostsArgs()
    {
      NodeName = name,
      Entry =
      [
        new HostsEntryArgs() { Address = "127.0.0.1", Hostnames = ["localhost.localdomain", "localhost"] },
        new HostsEntryArgs() { Address = args.InternalIpAddress, Hostnames = [$"{name}.driscoll.tech", $"{name}"] },
        new HostsEntryArgs() { Address = args.TailscaleIpAddress, Hostnames = [$"{name}.opossum-yo.ts.net"] },
        new HostsEntryArgs() { Address = "::1", Hostnames = ["ip6-localhost", "ip6-loopback"] },
        new HostsEntryArgs() { Address = "fe00::0", Hostnames = ["ip6-localnet"] },
        new HostsEntryArgs() { Address = "ff00::0", Hostnames = ["ip6-mcastprefix"] },
        new HostsEntryArgs() { Address = "ff02::1", Hostnames = ["ip6-allnodes"] },
        new HostsEntryArgs() { Address = "ff02::2", Hostnames = ["ip6-allrouters"] },
        new HostsEntryArgs() { Address = "ff02::3", Hostnames = ["ip6-allhosts"] },
      ]
    }, CustomResourceOptions.Merge(cro, new CustomResourceOptions()
    {
      Provider = PveProvider,
      ImportId = $"{name}/hosts"
    }));

    // tailscale job / pbs host
    // install jq

    var tailscaleScript = $$"""
                            #!/bin/bash
                            DIR="/etc/ssl/private"
                            NAME="$(tailscale status --json | jq '.Self.DNSName | .[:-1]' -r)"
                            tailscale cert --cert-file="${DIR}/${NAME}.crt" --key-file="${DIR}/${NAME}.key" "${NAME}"

                            # for PVE
                            pvenode cert set "${DIR}/${NAME}.crt" "${DIR}/${NAME}.key" --force --restart

                            """;

    if (args.IsBackupServer)
    {
      tailscaleScript = $$"""
                          {{tailscaleScript}}
                          # for PBS
                          cp ${DIR}/${NAME}.crt /etc/proxmox-backup/proxy.pem
                          cp ${DIR}/${NAME}.key /etc/proxmox-backup/proxy.key
                          chmod 640 /etc/proxmox-backup/proxy.key /etc/proxmox-backup/proxy.pem
                          chgrp backup /etc/proxmox-backup/proxy.key /etc/proxmox-backup/proxy.pem
                          systemctl reload proxmox-backup-proxy.service
                          """;
    }

    _ = new Pulumi.ProxmoxVE.Storage.File($"{name}-tailscale-job", new()
    {
      NodeName = name,
      DatastoreId = "local",
      FileMode = "755",
      Overwrite = true,
      ContentType = "backup",
      SourceRaw = new FileSourceRawArgs() { FileName = "/etc/cron.weekly/tailscale", Data = tailscaleScript },
    }, CustomResourceOptions.Merge(cro, new() { Provider = PveProvider }));
  }

  public Pulumi.ProxmoxVE.Provider PveProvider { get; }
  public Pulumi.Proxmox.Provider LxcProvider { get; }
}
