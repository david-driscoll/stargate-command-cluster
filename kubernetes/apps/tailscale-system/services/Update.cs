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

if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
{
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
}

if (serverKinds.Count == 0 && tailscaleStaticServices.Count == 0)
{
  Environment.Exit(0);
}

// ─────────────────────────────────────────────────────────────────────────────
// Helpers: build service name, external name, tailnet-fqdn for each kind
// ─────────────────────────────────────────────────────────────────────────────

static string ServiceName(string server, ServiceKind kind) => kind switch
{
  ServiceKind.Dockge => $"dockge-{server}",
  ServiceKind.Proxmox => $"proxmox-{server}",
  ServiceKind.Pbs => $"pbs-{server}",
  _ => throw new ArgumentOutOfRangeException(nameof(kind)),
};

static string ExternalName(string server, ServiceKind kind) => kind switch
{
  // dockge nodes live on the tailnet as "dockge-<server>"
  ServiceKind.Dockge => $"dockge-{server}",
  // proxmox and pbs share the same underlying node "<server>"
  ServiceKind.Proxmox => server,
  ServiceKind.Pbs => $"pbs-{server}",
  _ => throw new ArgumentOutOfRangeException(nameof(kind)),
};

static string TailnetFqdn(string server, ServiceKind kind) => $"{ExternalName(server, kind)}.${{TAILSCALE_DOMAIN}}";

// Returns the URL used in HTTP probes (includes port for non-standard 80/443)
static string ProbeHttpUrl(string server, ServiceKind kind, int port)
{
  var fqdn = TailnetFqdn(server, kind);
  return port == 443 ? $"https://{fqdn}" : $"https://{fqdn}:{port}";
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
    sb.AppendLine($"    - name: {p.Name}\n      port: {p.Port}\n      targetPort: {p.Port}");
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
      if (port.Name != "pve" && port.Name != "pbs")     // pve/pbs probes use the service name as-is
        probeName = $"{probeName}-{port.Name}";

      string target = port.ProbeModule == "ssh_banner"
          ? ProbeSshTarget(server, kind)
          : ProbeHttpUrl(server, kind, port.Port);

      sb.Append(ProbeYaml(probeName, port.ProbeModule!, target, isRemote));
    }

    // Per-kind alerts
    sb.Append(kind switch
    {
      ServiceKind.Dockge => DockgeAlertYaml(server, isRemote),
      ServiceKind.Proxmox => ProxmoxAlertYaml(server, ports.Any(p => p.Name == "ssh"), isRemote),
      ServiceKind.Pbs => PbsAlertYaml(server, isRemote),
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

enum ServiceKind { Dockge, Proxmox, Pbs }

record PortDef(string Name, int Port, bool HasProbe, string? ProbeModule, string? ProbePath = null);

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
