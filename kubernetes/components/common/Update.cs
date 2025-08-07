#!/usr/bin/dotnet run
// #:package YamlDotNet@16.3.0
#:package gstocco.YamlDotNet.YamlPath@1.0.26
#:package Spectre.Console@0.50.0
#:package Spectre.Console.Json@0.50.0
#:package KubernetesClient@*
#:package Microsoft.Extensions.Logging@9.*
#:package Dumpify@0.6.6
#:package Lunet.Extensions.Logging.SpectreConsole@1.2.0
#:package ProcessX@1.5.6
#:property JsonSerializerIsReflectionEnabledByDefault=true
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Cysharp.Diagnostics;
using Dumpify;
using gfs.YamlDotNet.YamlPath;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Lunet.Extensions.Logging.SpectreConsole;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

var serializer = new SerializerBuilder().Build();
var factory = LoggerFactory.Create(configure =>
           {
             configure.AddSpectreConsole(new SpectreConsoleLoggerOptions()
             {
               IncludeNewLineBeforeMessage = false,
               IncludeTimestamp = true,
             });
           }
       );
try
{
  var localCluster = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile("./kubeconfig"));

  var result = await localCluster.CustomObjects.GetClusterCustomObjectAsync<TailscaleDns>("tailscale.com", "v1alpha1", "dnsconfigs", "ts-dns");

  var clusterConfig = await ReadStream("kubernetes/components/common/cluster-secrets.sops.yaml").OfType<YamlMappingNode>().SingleAsync();
  var clusterCname = clusterConfig.Query("/stringData/TAILSCALE_NAMESERVER_IP").OfType<YamlScalarNode>().Single();

  if (clusterCname.Value != result.Status.Nameserver.Ip)
  {
    clusterCname.Value = result.Status.Nameserver.Ip;
    // await WriteFile("kubernetes/components/common/cluster-secrets.sops.yaml", serializer.Serialize(clusterConfig));
    AnsiConsole.WriteLine("Updated TAILSCALE_NAMESERVER_IP to {0}", result.Status.Nameserver.Ip);
  }
}
catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
{
  AnsiConsole.WriteLine("Tailscale DNS configuration not found. Please ensure the Tailscale operator is running and the DNS configuration is created.");
}

static async IAsyncEnumerable<YamlMappingNode> ReadStream(string path)
{
  var doc = new YamlStream();
  using var reader = new StringReader(await ReadFile(path));
  doc.Load(reader);

  var rootNodes = doc.Documents
  .Select(z => (z.RootNode as YamlMappingNode)!)
  .Where(z => z is not null);
  foreach (var item in rootNodes)
  {
    yield return item;
  }
}

static async ValueTask<string> ReadFile(string path)
{
  return path.EndsWith(".sops.yaml", StringComparison.OrdinalIgnoreCase) ? (await ProcessX.StartAsync($"sops -d {path}".Dump(), workingDirectory: Directory.GetCurrentDirectory())
    .AggregateAsync(new StringBuilder(), (sb, line, ct) =>
  {
    sb.AppendLine(line);
    return ValueTask.FromResult(sb);
  }, CancellationToken.None)).ToString() : File.ReadAllText(path);
}


static async ValueTask WriteFile(string path, string content)
{
  await File.WriteAllTextAsync(path, content);
  if (path.EndsWith(".sops.yaml", StringComparison.OrdinalIgnoreCase))
  {
    await ProcessX.StartAsync($"sops -e -i {path}".Dump(), workingDirectory: Directory.GetCurrentDirectory()).ToTask();
  }
}


public class TailscaleDns : KubernetesObject, IMetadata<V1ObjectMeta>
{
  [JsonPropertyName("metadata")]
  public V1ObjectMeta Metadata { get; set; }

  [JsonPropertyName("status")]
  public TailscaleDnsStatus Status { get; set; }
}

public class TailscaleDnsStatus
{
  [JsonPropertyName("nameserver")]
  public TailscaleDnsStatusNameserver Nameserver { get; set; }
}

public class TailscaleDnsStatusNameserver
{
  [JsonPropertyName("ip")]
  public string Ip { get; set; }
}
