#!/usr/bin/dotnet run
#:package YamlDotNet@18.1.0
#:package Spectre.Console@0.57.2
#:package System.Net.Http.Json@10.*
#:package Duende.IdentityModel@8.1.0

#:property PublishAot=false

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Spectre.Console;

var serializer = new YamlDotNet.Serialization.Serializer();

// ─────────────────────────────────────────────────────────────────────────────
// Service kind and per-type defaults
// ─────────────────────────────────────────────────────────────────────────────

// Default ports for each service kind (portName, portNumber, probe?)
var defaultPorts = new Dictionary<ServiceKind, List<PortDef>>
{
  [ServiceKind.Dockge] = [new("https", 443, false, null), new("ssh", 22, true, "ssh_banner")],
  [ServiceKind.Proxmox] = [new("pve", 8006, true, "http_2xx"), new("ssh", 22, true, "ssh_banner")],
  [ServiceKind.Pbs] = [new("pbs", 8007, true, "http_2xx"), new("ssh", 22, true, "ssh_banner")],
  [ServiceKind.Dns] = [
    new("dns-tcp", 53, true, "dns_soa"),
    new("dns-udp", 53, false, null, Protocol: "UDP"),
    new("dot", 853, true, "tcp_connect"),
    new("doq", 853, false, null, Protocol: "UDP"),
    // DoH is the one probe that must target the split-horizon name: Technitium
    // serves a Let's Encrypt cert whose only SAN is "<server>.dns.driscoll.tech",
    // so probing the tailnet name fails TLS hostname verification. Inside the
    // cluster CoreDNS rewrites "<server>.dns.${ROOT_DOMAIN}" back to the tailnet
    // name and forwards to the Tailscale operator nameserver, so this resolves
    // over MagicDNS to the same endpoint and stays independent of Technitium.
    new("doh", 443, true, "http_2xx", ProbeDomain: "dns.${ROOT_DOMAIN}"),
    // Technitium admin/API TLS port — used by the pulumi operator's technitium provider
    new("admin", 53443, false, null),
  ],
};

var remoteMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
{
  ["skystar"] = true,
  // ["luna"] = true,
};

// Maps Tailscale tag → (ServiceKind, fn to extract the physical server name)
var tagMap = new Dictionary<string, (ServiceKind Kind, Func<string, string> ServerName)>
{
  ["tag:dockge"] = (ServiceKind.Dockge, h => h.StartsWith("dockge-") ? h["dockge-".Length..] : h),
  ["tag:proxmox"] = (ServiceKind.Proxmox, h => h),
  ["tag:backups"] = (ServiceKind.Pbs, h => h.StartsWith("pbs-") ? h["pbs-".Length..] : h),
  ["tag:dns"] = (ServiceKind.Dns, h => h.StartsWith("dns-") ? h["dns-".Length..] : h),
};

// ─────────────────────────────────────────────────────────────────────────────
// Per-device overrides
//   excludePorts: which default port names to suppress for a given server+kind
//   extraPorts:   additional ports to add for a given server+kind
// ─────────────────────────────────────────────────────────────────────────────

var extraPorts = new Dictionary<string, Dictionary<ServiceKind, PortDef[]>>
{
  // alpha-site hosts a NUT UPS daemon on 3493
  ["alpha-site"] = new()
  {
    [ServiceKind.Proxmox] = [new("nut", 3493, false, null)],
  },
  // as hosts a primary adguard host on 4000
  ["as"] = new()
  {
    [ServiceKind.Dockge] = [new("adguard", 4000, false, null)]
  },
  // luna hosts the Home Assistant voice pipeline: llama.cpp (gemma-4-E2B) on
  // 8080 and wyoming whisper/piper STT/TTS on 10300/10200. HA runs on this
  // cluster (hostNetwork) and reaches them through this egress service — the
  // dockge-* DNS names resolve to tailnet IPs that SGC nodes cannot route.
  ["luna"] = new()
  {
    [ServiceKind.Dockge] = [
      new("llm", 8080, false, null),
      new("stt", 10300, false, null),
      new("tts", 10200, false, null),
    ]
  },
  // celestia hosts llama-agent (gemma-4-E4B, Hermes' backend) on 8081
  ["celestia"] = new()
  {
    [ServiceKind.Dockge] = [new("llm-agent", 8081, false, null)]
  },
};

// ─────────────────────────────────────────────────────────────────────────────
// Static Tailscale services (exposed via Tailscale operator, not physical devices)
// ─────────────────────────────────────────────────────────────────────────────

var tailscaleStaticServices = new List<TailscaleServiceDef>
{
  // new("alertmanager",   [new("http", 9093,  true, "http_2xx")]),
  // new("loki",           [new("http", 3100,  true, "http_2xx")]),
  // new("thanos-receive", [new("http", 10902, true, "http_2xx"), new("grpc", 10901, false, null)]),
  new ("sgc-kubeproxy", [new ("https", 443, true, "http_2xx", "/healthz")]),
  new ("equestria-kubeproxy", [new ("https", 443, true, "http_2xx", "/healthz")]),
};

// ─────────────────────────────────────────────────────────────────────────────
// Build server → kinds map from Tailscale API (with static fallback)
// ─────────────────────────────────────────────────────────────────────────────

// serverName → ordered set of kinds
var serverKinds = new Dictionary<string, SortedSet<ServiceKind>>(StringComparer.OrdinalIgnoreCase);

var clientId = Environment.GetEnvironmentVariable("TAILSCALE_CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("TAILSCALE_CLIENT_SECRET");
var tailnet = Environment.GetEnvironmentVariable("TAILSCALE_TAILNET") ?? "-";

// Fail CLOSED when credentials are absent.
//
// The device list is the only input that decides which per-device files exist. If we
// skipped the API call and carried on with an empty `serverKinds`, the generator would
// happily rewrite kustomization.yaml with nothing but the static services and drop every
// per-device Service/Probe/PrometheusRule — and because these files are Flux-managed,
// that prunes the live objects for the whole tailnet, monitoring included.
//
// Exit NON-ZERO (not 0) so `mise run update` fails loudly and the pre-commit hook aborts
// the commit, rather than letting a destructive regeneration look like an intentional diff.
if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
{
  AnsiConsole.MarkupLine("[red]TAILSCALE_CLIENT_ID / TAILSCALE_CLIENT_SECRET are not set — refusing to regenerate.[/]");
  AnsiConsole.MarkupLine("[red]Regenerating without the Tailscale API would delete every per-device service, probe and alert.[/]");
  AnsiConsole.MarkupLine("[yellow]Run through 1Password (e.g. `op run --no-masking -- mise run update`) so the credentials resolve.[/]");
  AnsiConsole.MarkupLine("[yellow]No files were written.[/]");
  Environment.Exit(1);
}

// .mise.toml sets both variables to literal `op://…` references that only `op run` expands.
// Running this script directly under mise (without `op run`) therefore yields credentials that
// are non-empty but unusable — which would sail past the emptiness check above and fail later
// as an opaque OAuth error. Name the real problem instead.
if (clientId.StartsWith("op://", StringComparison.OrdinalIgnoreCase) ||
    clientSecret.StartsWith("op://", StringComparison.OrdinalIgnoreCase))
{
  AnsiConsole.MarkupLine("[red]Tailscale credentials are unresolved 1Password references (`op://…`) — refusing to regenerate.[/]");
  AnsiConsole.MarkupLine("[yellow]Wrap the command in `op run --no-masking --` so 1Password expands them.[/]");
  AnsiConsole.MarkupLine("[yellow]No files were written.[/]");
  Environment.Exit(1);
}

AnsiConsole.MarkupLine("[blue]Fetching Tailscale devices from API...[/]");
try
{
  using var http = new HttpClient();
  var tokenResponse = await http.RequestTokenAsync(new TokenRequest()
  {
    Address = "https://api.tailscale.com/api/v2/oauth/token",
    GrantType = OidcConstants.GrantTypes.ClientCredentials,
    ClientId = clientId,
    ClientSecret = clientSecret,
  });
  AnsiConsole.MarkupLine("[green]Successfully obtained access token from Tailscale API[/]");

  http.DefaultRequestHeaders.Authorization = new("Bearer", tokenResponse.AccessToken);
  var response = await http.GetFromJsonAsync<TailscaleDevicesResponse>(
      $"https://api.tailscale.com/api/v2/tailnet/{tailnet}/devices",
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

  foreach (var device in response!.Devices)
  {
    AnsiConsole.MarkupLine($"[blue]Processing device: {device.Hostname}[/]");
    foreach (var tag in device.Tags ?? [])
    {
      AnsiConsole.MarkupLine($"[blue]  Checking tag: {tag}[/]");
      if (!tagMap.TryGetValue(tag, out var mapping)) continue;
      var server = mapping.ServerName(device.Hostname);
      if (!serverKinds.TryGetValue(server, out var kinds))
        serverKinds[server] = kinds = [];
      kinds.Add(mapping.Kind);
    }
  }
  AnsiConsole.MarkupLine($"[green]Found {serverKinds.Count} servers from Tailscale API[/]");
}
catch (Exception ex)
{
  AnsiConsole.WriteException(ex, new ExceptionSettings { Format = ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes | ExceptionFormats.ShortenMethods });
  Environment.Exit(0);
}

// Credentials were present and the API answered, but no tagged device came back. That is a
// different failure from "no credentials" — most likely the OAuth client lost its scopes or
// the tag mapping drifted — and it is still not a reason to prune everything.
if (serverKinds.Count == 0)
{
  AnsiConsole.MarkupLine("[red]The Tailscale API returned no devices matching any known tag — refusing to regenerate.[/]");
  AnsiConsole.MarkupLine($"[red]Expected tags: {string.Join(", ", tagMap.Keys)} on tailnet '{tailnet}'.[/]");
  AnsiConsole.MarkupLine("[yellow]No files were written.[/]");
  Environment.Exit(1);
}

// ─────────────────────────────────────────────────────────────────────────────
// Helpers: build service name, external name, tailnet-fqdn for each kind
// ─────────────────────────────────────────────────────────────────────────────

static string ServiceName(string server, ServiceKind kind) => kind switch
{
  ServiceKind.Dockge => $"dockge-{server}",
  ServiceKind.Proxmox => $"proxmox-{server}",
  ServiceKind.Pbs => $"pbs-{server}",
  ServiceKind.Dns => $"dns-{server}",
  _ => throw new ArgumentOutOfRangeException(nameof(kind)),
};

static string ExternalName(string server, ServiceKind kind) => kind switch
{
  // dockge nodes live on the tailnet as "dockge-<server>"
  ServiceKind.Dockge => $"dockge-{server}",
  // proxmox and pbs share the same underlying node "<server>"
  ServiceKind.Proxmox => server,
  ServiceKind.Pbs => $"pbs-{server}",
  // technitium nodes live on the tailnet as "dns-<server>"
  ServiceKind.Dns => $"dns-{server}",
  _ => throw new ArgumentOutOfRangeException(nameof(kind)),
};

static string TailnetFqdn(string server, ServiceKind kind) => $"{ExternalName(server, kind)}.${{TAILSCALE_DOMAIN}}";

// The hostname a probe should target. Defaults to the tailnet FQDN; a port may
// opt into a different domain via PortDef.ProbeDomain, which is appended to the
// bare server name (e.g. "celestia" + "dns.${ROOT_DOMAIN}").
static string ProbeFqdn(string server, ServiceKind kind, PortDef port)
  => port.ProbeDomain is { } domain
    ? $"{server}.{domain}"
    : TailnetFqdn(server, kind);

// Returns the URL used in HTTP probes (includes port for non-standard 80/443)
static string ProbeHttpUrl(string server, ServiceKind kind, PortDef port)
{
  var fqdn = ProbeFqdn(server, kind, port);
  return port.Port == 443 ? $"https://{fqdn}" : $"https://{fqdn}:{port.Port}";
}

static string ProbeSshTarget(string server, ServiceKind kind) => $"{TailnetFqdn(server, kind)}:22";

// ─────────────────────────────────────────────────────────────────────────────
// YAML generators
// ─────────────────────────────────────────────────────────────────────────────

string ServiceYaml(string server, ServiceKind kind, List<PortDef> ports)
{
  var svcName = ServiceName(server, kind);
  var extName = ExternalName(server, kind);
  var fqdn = TailnetFqdn(server, kind);
  var sb = new StringBuilder();

  sb.AppendLine("---");
  sb.AppendLine($"# yaml-language-server: $schema=https://raw.githubusercontent.com/yannh/kubernetes-json-schema/refs/heads/master/v1.34.2/service.json");
  sb.AppendLine($"apiVersion: v1");
  sb.AppendLine($"kind: Service");
  sb.AppendLine($"metadata:");
  sb.AppendLine($"  name: {svcName}");
  sb.AppendLine($"  annotations:");
  sb.AppendLine($"    tailscale.com/tailnet-fqdn: \"{fqdn}\"");
  sb.AppendLine($"    tailscale.com/proxy-group: tailnet-inbound");
  sb.AppendLine($"spec:");
  sb.AppendLine($"  type: ExternalName");
  sb.AppendLine($"  externalName: {extName}");
  sb.AppendLine($"  ports:");
  foreach (var p in ports)
  {
    sb.AppendLine($"    - name: {p.Name}\n      port: {p.Port}\n      targetPort: {p.Port}");
    if (p.Protocol is not null)
      sb.AppendLine($"      protocol: {p.Protocol}");
  }
  return sb.ToString();
}

string ProbeYaml(string probeName, string module, string target, bool isRemote = false)
{
  var sb = new StringBuilder();
  sb.AppendLine("---");
  sb.AppendLine($"apiVersion: monitoring.coreos.com/v1");
  sb.AppendLine($"kind: Probe");
  sb.AppendLine($"metadata:");
  sb.AppendLine($"  name: {probeName}");
  sb.AppendLine($"spec:");
  sb.AppendLine($"  interval: 2m");
  sb.AppendLine($"  module: {module}");
  sb.AppendLine($"  prober:");
  sb.AppendLine($"    url: blackbox-exporter.observability.svc.cluster.local:9115");
  sb.AppendLine($"  targets:");
  sb.AppendLine($"    staticConfig:");
  sb.AppendLine($"      static:");
  sb.AppendLine($"        - {target}");
  if (isRemote)
  {
    sb.AppendLine($"      labels:");
    sb.AppendLine($"        remote: \"true\"");
  }
  return sb.ToString();
}

// ─────────────────────────────────────────────────────────────────────────────
// Alert generators per service kind
// ─────────────────────────────────────────────────────────────────────────────

string DockgeAlertYaml(string server, bool isRemote)
{
  var sb = new StringBuilder();
  sb.AppendLine("---");
  sb.AppendLine($"apiVersion: monitoring.coreos.com/v1");
  sb.AppendLine($"kind: PrometheusRule");
  sb.AppendLine($"metadata:");
  sb.AppendLine($"  name: dockge-{server}-alerts");
  sb.AppendLine($"spec:");
  sb.AppendLine($"  groups:");
  sb.AppendLine($"    - name: dockge-{server}");
  sb.AppendLine($"      rules:");
  sb.AppendLine($"        - alert: DockgeSSHConnectivityLost");
  sb.AppendLine($"          annotations:");
  sb.AppendLine($"            description: \"SSH connectivity to Dockge on {server} has been lost.\"");
  sb.AppendLine($"            summary: \"Dockge {server} SSH lost\"");
  sb.AppendLine($"          expr: |");
  sb.AppendLine($"            probe_success{{probe=\"dockge-{server}-ssh\"}} < 1");
  sb.AppendLine($"          for: {(isRemote ? "2h" : "10m")}");
  sb.AppendLine($"          labels:");
  sb.AppendLine($"            severity: warning");
  return sb.ToString();
}

string ProxmoxAlertYaml(string server, bool hasSsh, bool isRemote)
{
  var sb = new StringBuilder();
  sb.AppendLine("---");
  sb.AppendLine($"apiVersion: monitoring.coreos.com/v1");
  sb.AppendLine($"kind: PrometheusRule");
  sb.AppendLine($"metadata:");
  sb.AppendLine($"  name: proxmox-{server}-alerts");
  sb.AppendLine($"spec:");
  sb.AppendLine($"  groups:");
  sb.AppendLine($"    - name: proxmox-{server}");
  sb.AppendLine($"      rules:");
  sb.AppendLine($"        - alert: ProxmoxServiceUnhealthy");
  sb.AppendLine($"          annotations:");
  sb.AppendLine($"            description: \"Proxmox VE on {server} is unhealthy.\"");
  sb.AppendLine($"            summary: \"Proxmox {server} is unhealthy\"");
  sb.AppendLine($"          expr: |");
  sb.AppendLine($"            probe_success{{probe=\"proxmox-{server}\"}} < 1");
  sb.AppendLine($"          for: {(isRemote ? "2h" : "10m")}");
  sb.AppendLine($"          labels:");
  sb.AppendLine($"            severity: warning");
  if (hasSsh)
  {
    sb.AppendLine($"        - alert: ProxmoxSSHConnectivityLost");
    sb.AppendLine($"          annotations:");
    sb.AppendLine($"            description: \"SSH connectivity to Proxmox on {server} has been lost.\"");
    sb.AppendLine($"            summary: \"Proxmox {server} SSH lost\"");
    sb.AppendLine($"          expr: |");
    sb.AppendLine($"            probe_success{{probe=\"proxmox-{server}-ssh\"}} < 1");
    sb.AppendLine($"          for: {(isRemote ? "2h" : "10m")}");
    sb.AppendLine($"          labels:");
    sb.AppendLine($"            severity: warning");
  }
  return sb.ToString();
}

string PbsAlertYaml(string server, bool isRemote)
{
  var sb = new StringBuilder();
  sb.AppendLine("---");
  sb.AppendLine($"apiVersion: monitoring.coreos.com/v1");
  sb.AppendLine($"kind: PrometheusRule");
  sb.AppendLine($"metadata:");
  sb.AppendLine($"  name: pbs-{server}-alerts");
  sb.AppendLine($"spec:");
  sb.AppendLine($"  groups:");
  sb.AppendLine($"    - name: pbs-{server}");
  sb.AppendLine($"      rules:");
  sb.AppendLine($"        - alert: PBSSSHConnectivityLost");
  sb.AppendLine($"          annotations:");
  sb.AppendLine($"            description: \"SSH connectivity to PBS on {server} has been lost.\"");
  sb.AppendLine($"            summary: \"PBS {server} SSH lost\"");
  sb.AppendLine($"          expr: |");
  sb.AppendLine($"            probe_success{{probe=\"pbs-{server}-ssh\"}} < 1");
  sb.AppendLine($"          for: {(isRemote ? "2h" : "10m")}");
  sb.AppendLine($"          labels:");
  sb.AppendLine($"            severity: warning");
  return sb.ToString();
}

string DnsAlertYaml(string server, bool isRemote)
{
  var sb = new StringBuilder();
  sb.AppendLine("---");
  sb.AppendLine($"apiVersion: monitoring.coreos.com/v1");
  sb.AppendLine($"kind: PrometheusRule");
  sb.AppendLine($"metadata:");
  sb.AppendLine($"  name: dns-{server}-alerts");
  sb.AppendLine($"spec:");
  sb.AppendLine($"  groups:");
  sb.AppendLine($"    - name: dns-{server}");
  sb.AppendLine($"      rules:");
  sb.AppendLine($"        - alert: TechnitiumDnsUnhealthy");
  sb.AppendLine($"          annotations:");
  sb.AppendLine($"            description: \"Technitium DNS on {server} is not answering SOA queries.\"");
  sb.AppendLine($"            summary: \"Technitium DNS {server} unhealthy\"");
  sb.AppendLine($"          expr: |");
  sb.AppendLine($"            probe_success{{probe=\"dns-{server}\"}} < 1");
  sb.AppendLine($"          for: {(isRemote ? "2h" : "10m")}");
  sb.AppendLine($"          labels:");
  sb.AppendLine($"            severity: critical");
  return sb.ToString();
}

// ─────────────────────────────────────────────────────────────────────────────
// YAML generators for static Tailscale services
// ─────────────────────────────────────────────────────────────────────────────

string TailscaleStaticServiceYaml(TailscaleServiceDef svc)
{
  var fqdn = $"{svc.Name}.${{TAILSCALE_DOMAIN}}";
  var sb = new StringBuilder();
  sb.AppendLine("---");
  sb.AppendLine($"# yaml-language-server: $schema=https://raw.githubusercontent.com/yannh/kubernetes-json-schema/refs/heads/master/v1.34.2/service.json");
  sb.AppendLine($"apiVersion: v1");
  sb.AppendLine($"kind: Service");
  sb.AppendLine($"metadata:");
  sb.AppendLine($"  name: {svc.Name}");
  sb.AppendLine($"  annotations:");
  sb.AppendLine($"    tailscale.com/tailnet-fqdn: \"{fqdn}\"");
  sb.AppendLine($"    tailscale.com/proxy-group: tailnet-inbound");
  sb.AppendLine($"spec:");
  sb.AppendLine($"  type: ExternalName");
  sb.AppendLine($"  externalName: {svc.Name}");
  sb.AppendLine($"  ports:");
  foreach (var p in svc.Ports)
    sb.AppendLine($"    - name: {p.Name}\n      port: {p.Port}\n      targetPort: {p.Port}");
  return sb.ToString();
}

static string StaticServiceProbeUrl(string name, int port, string? path = null)
  => port == 443
    ? $"https://{name}.${{TAILSCALE_DOMAIN}}{path ?? ""}"
    : $"https://{name}.${{TAILSCALE_DOMAIN}}:{port}{path ?? ""}";

// ─────────────────────────────────────────────────────────────────────────────
// Generate per-device YAML files
// ─────────────────────────────────────────────────────────────────────────────

var outputDir = "kubernetes/apps/tailscale-system/services";
var generatedFiles = new List<string>();

// ─────────────────────────────────────────────────────────────────────────────
// Sanity floor — nothing has been written yet, and this is the last chance to bail.
//
// The credential guard above catches a *total* loss of the device list. This catches a
// *partial* one: a degraded API response, an OAuth client that lost visibility of some
// tags, or a tailnet mid-migration. Any of those would quietly prune live Flux-managed
// objects. Losing a device or two is normal decommissioning; losing a third of the estate
// in a single run is a bug somewhere upstream.
//
// Override with TAILSCALE_UPDATE_ALLOW_SHRINK=1 for a genuine bulk decommission.
// ─────────────────────────────────────────────────────────────────────────────

// Tolerate shrinking to 70% of the previous device count; refuse below that.
const double MinDeviceRetentionRatio = 0.7;

var previousDeviceCount = CountPreviousDeviceFiles(outputDir, tailscaleStaticServices);
var allowShrink = Environment.GetEnvironmentVariable("TAILSCALE_UPDATE_ALLOW_SHRINK") == "1";

if (previousDeviceCount > 0 && serverKinds.Count < previousDeviceCount * MinDeviceRetentionRatio && !allowShrink)
{
  AnsiConsole.MarkupLine($"[red]Device count dropped from {previousDeviceCount} to {serverKinds.Count} — refusing to regenerate.[/]");
  AnsiConsole.MarkupLine($"[red]That is below the {MinDeviceRetentionRatio:P0} sanity floor and would prune live services, probes and alerts.[/]");
  AnsiConsole.MarkupLine($"[yellow]Devices seen this run: {string.Join(", ", serverKinds.Keys.OrderBy(x => x))}[/]");
  AnsiConsole.MarkupLine("[yellow]If this shrink is intentional, re-run with TAILSCALE_UPDATE_ALLOW_SHRINK=1. No files were written.[/]");
  Environment.Exit(1);
}

// Counts the per-device files the previous run registered in kustomization.yaml, i.e.
// every listed resource that is neither hand-maintained nor a static Tailscale service.
static int CountPreviousDeviceFiles(string dir, List<TailscaleServiceDef> statics)
{
  var kustomization = Path.Combine(dir, "kustomization.yaml");
  if (!File.Exists(kustomization)) return 0;

  var notADevice = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
  {
    "tailscale.yaml",      // hand-maintained
    "prometheusrule.yaml", // shared generic rules
  };
  foreach (var svc in statics) notADevice.Add($"{svc.Name}.yaml");

  return File.ReadLines(kustomization)
    .Select(line => line.Trim())
    .Where(line => line.StartsWith("- ./") && line.EndsWith(".yaml"))
    .Select(line => line["- ./".Length..])
    .Count(file => !notADevice.Contains(file));
}

foreach (var (server, kinds) in serverKinds.OrderBy(x => x.Key))
{
  var sb = new StringBuilder();
  sb.AppendLine($"# Generated by Update.cs — do not edit manually");
  sb.AppendLine($"# Server: {server}  |  Services: {string.Join(", ", kinds)}");

  foreach (var kind in kinds)
  {
    // Resolve ports for this server+kind
    var ports = defaultPorts[kind].ToList();
    if (extraPorts.TryGetValue(server, out var kindExtras) &&
        kindExtras.TryGetValue(kind, out var extras))
      ports.AddRange(extras);

    // Service
    sb.Append(ServiceYaml(server, kind, ports));

    var isRemote = remoteMap.TryGetValue(server, out var remote) && remote;

    // Probes
    foreach (var port in ports.Where(p => p.HasProbe))
    {
      var probeName = $"{ServiceName(server, kind)}";
      if (port.Name != "pve" && port.Name != "pbs" && port.Name != "dns-tcp")     // pve/pbs/dns probes use the service name as-is
        probeName = $"{probeName}-{port.Name}";

      string target = port.ProbeModule switch
      {
        "ssh_banner" => ProbeSshTarget(server, kind),
        "http_2xx" => ProbeHttpUrl(server, kind, port),
        // tcp_connect / dns_soa probe the raw host:port
        _ => $"{ProbeFqdn(server, kind, port)}:{port.Port}",
      };

      sb.Append(ProbeYaml(probeName, port.ProbeModule!, target, isRemote));
    }

    // Per-kind alerts
    sb.Append(kind switch
    {
      ServiceKind.Dockge => DockgeAlertYaml(server, isRemote),
      ServiceKind.Proxmox => ProxmoxAlertYaml(server, ports.Any(p => p.Name == "ssh"), isRemote),
      ServiceKind.Pbs => PbsAlertYaml(server, isRemote),
      ServiceKind.Dns => DnsAlertYaml(server, isRemote),
      _ => ""
    });
  }

  var fileName = $"{server}.yaml";
  await File.WriteAllTextAsync(Path.Combine(outputDir, fileName), sb.ToString());
  generatedFiles.Add(fileName);
  AnsiConsole.MarkupLine($"[green]Generated {fileName}[/]");
}

// ─────────────────────────────────────────────────────────────────────────────
// Generate static Tailscale service files
// ─────────────────────────────────────────────────────────────────────────────

foreach (var svc in tailscaleStaticServices)
{
  var sb = new StringBuilder();
  sb.AppendLine($"# Generated by Update.cs — do not edit manually");
  sb.AppendLine($"# Tailscale service: {svc.Name}");

  sb.Append(TailscaleStaticServiceYaml(svc));

  foreach (var port in svc.Ports.Where(p => p.HasProbe))
  {
    var probeName = svc.Ports.Count(p => p.HasProbe) > 1
        ? $"{svc.Name}-{port.Name}"
        : svc.Name;
    sb.Append(ProbeYaml(probeName, port.ProbeModule!, StaticServiceProbeUrl(svc.Name, port.Port, port.ProbePath)));
  }

  var fileName = $"{svc.Name}.yaml";
  await File.WriteAllTextAsync(Path.Combine(outputDir, fileName), sb.ToString());
  generatedFiles.Add(fileName);
  AnsiConsole.MarkupLine($"[green]Generated {fileName}[/]");
}

// ─────────────────────────────────────────────────────────────────────────────
// Generate shared generic PrometheusRule (blackbox recording rules + alerts)
// ─────────────────────────────────────────────────────────────────────────────

const string PrometheusRuleFile = "prometheusrule.yaml";
await File.WriteAllTextAsync(Path.Combine(outputDir, PrometheusRuleFile), """
# Generated by Update.cs — do not edit manually
---
apiVersion: monitoring.coreos.com/v1
kind: PrometheusRule
metadata:
  name: blackbox-probe-alerts
spec:
  groups:
    - name: blackbox_probes
      interval: 2m
      rules:
        - record: blackbox:probe:success:rate10m
          expr: |
            avg by (probe) (rate(probe_success[10m]))
        - alert: BlackboxProbeFailing
          annotations:
            description: "Blackbox probe '{{ $labels.instance }}' has been failing."
            summary: "Blackbox probe failing"
          expr: |
            avg_over_time(probe_success{remote!="true"}[10m]) < 0.9
          for: 10m
          labels:
            severity: warning
        - alert: BlackboxProbeFailingCritical
          annotations:
            description: "Blackbox probe '{{ $labels.instance }}' is critically failing."
            summary: "Blackbox probe critically failing"
          expr: |
            avg_over_time(probe_success{remote!="true"}[10m]) < 0.5
          for: 2m
          labels:
            severity: critical
        - alert: BlackboxProbeFailingRemote
          annotations:
            description: "Blackbox probe '{{ $labels.instance }}' (remote) has been failing."
            summary: "Blackbox probe failing (remote)"
          expr: |
            avg_over_time(probe_success{remote="true"}[10m]) < 0.9
          for: 2h
          labels:
            severity: warning
        - alert: BlackboxProbeFailingCriticalRemote
          annotations:
            description: "Blackbox probe '{{ $labels.instance }}' (remote) is critically failing."
            summary: "Blackbox probe critically failing (remote)"
          expr: |
            avg_over_time(probe_success{remote="true"}[10m]) < 0.5
          for: 30m
          labels:
            severity: critical
        - alert: BlackboxProbeHighLatency
          annotations:
            description: "Blackbox probe '{{ $labels.instance }}' p99 latency is {{ $value | humanizeDuration }}."
            summary: "Blackbox probe high latency"
          expr: |
            histogram_quantile(0.99, sum by (probe, le) (rate(probe_duration_seconds_bucket{remote!="true"}[10m]))) > 10
          for: 10m
          labels:
            severity: warning
        - alert: BlackboxProbeSslCertificateExpiringSoon
          annotations:
            description: "SSL certificate for probe '{{ $labels.instance }}' expires in {{ $value | humanizeDuration }}."
            summary: "SSL certificate expiring soon"
          expr: |
            probe_ssl_earliest_cert_expiry - time() < 86400 * 7
          for: 1h
          labels:
            severity: warning
        - alert: BlackboxProbeTimeout
          annotations:
            description: "Blackbox probe '{{ $labels.instance }}' is timing out."
            summary: "Blackbox probe timeout"
          expr: |
            probe_success{remote!="true"} == 0 and probe_duration_seconds{remote!="true"} > 9
          for: 2m
          labels:
            severity: warning
""");
AnsiConsole.MarkupLine($"[green]Generated {PrometheusRuleFile}[/]");

// ─────────────────────────────────────────────────────────────────────────────
// Rewrite kustomization.yaml
// ─────────────────────────────────────────────────────────────────────────────

var kustomizationContent = new StringBuilder();
kustomizationContent.AppendLine("---");
kustomizationContent.AppendLine("# yaml-language-server: $schema=https://json.schemastore.org/kustomization");
kustomizationContent.AppendLine("apiVersion: kustomize.config.k8s.io/v1beta1");
kustomizationContent.AppendLine("kind: Kustomization");
kustomizationContent.AppendLine("resources:");
kustomizationContent.AppendLine("  - ./tailscale.yaml");
kustomizationContent.AppendLine("  - ./prometheusrule.yaml");
foreach (var f in generatedFiles)
  kustomizationContent.AppendLine($"  - ./{f}");
await File.WriteAllTextAsync(Path.Combine(outputDir, "kustomization.yaml"), kustomizationContent.ToString());
AnsiConsole.MarkupLine("[green]Updated kustomization.yaml[/]");

// ─────────────────────────────────────────────────────────────────────────────
// Types
// ─────────────────────────────────────────────────────────────────────────────

enum ServiceKind { Dockge, Proxmox, Pbs, Dns }

// ProbeDomain: optional per-port override for the probe hostname. When set, the
// probe targets "<server>.<ProbeDomain>" instead of the tailnet FQDN. Use it
// where the endpoint presents a certificate for a name other than its tailnet
// name; leave it null so probes stay on the tailnet FQDN.
record PortDef(string Name, int Port, bool HasProbe, string? ProbeModule, string? ProbePath = null, string? Protocol = null, string? ProbeDomain = null);

record TailscaleServiceDef(string Name, List<PortDef> Ports);

class TailscaleDevicesResponse
{
  [JsonPropertyName("devices")]
  public List<TailscaleDevice> Devices { get; set; } = [];
}

class TailscaleDevice
{
  [JsonPropertyName("hostname")]
  public string Hostname { get; set; } = "";

  [JsonPropertyName("tags")]
  public List<string>? Tags { get; set; }
}
