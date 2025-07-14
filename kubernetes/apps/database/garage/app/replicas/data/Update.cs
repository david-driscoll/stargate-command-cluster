#!/usr/bin/dotnet run
#:package YamlDotNet@16.3.0
#:package gstocco.YamlDotNet.YamlPath@1.0.26
#:package System.Collections.Immutable@10.0.0-preview.5.25277.114
#:package Spectre.Console@0.50.0
#:package Spectre.Console.Json@0.50.0
#:package Dumpify@0.6.6
#:package ProcessX@1.5.6
#:package System.Interactive@6.0.3
#:package System.Interactive.Async@6.0.3

using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Diagnostics;
using Dumpify;
using gfs.YamlDotNet.YamlPath;
using Spectre.Console;
using Spectre.Console.Advanced;
using Spectre.Console.Json;
using YamlDotNet;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

Dictionary<string, string> defaults = new Dictionary<string, string>()
{
  { "VOLSYNC_CAPACITY", "64Gi" },
  // { "VOLSYNC_CACHE_CAPACITY", "" },
  { "VOLSYNC_STORAGECLASS", "longhorn-local" },
  // { "VOLSYNC_COPYMETHOD", "" },
  // { "VOLSYNC_SNAPSHOTCLASS", "" },
  // { "VOLSYNC_CACHE_ACCESSMODES", "ReadWriteOnce" },
  // { "VOLSYNC_CACHE_SNAPSHOTCLASS", "" },
  // { "VOLSYNC_PUID", 568 },
  // { "VOLSYNC_PGID", 568 },
};

var filePath = "kubernetes/apps/database/garage/app/replicas/data/kustomization.yaml";

var databasesContent = new StringBuilder();

var clusterConfig = await ReadStream("kubernetes/components/common/cluster-secrets.sops.yaml");
int servers = int.Parse(clusterConfig.OfType<YamlMappingNode>().Single().Query("/stringData/REPLICA_NODES").OfType<YamlScalarNode>().Single().Value);

var secretTemplate = await GetTemplate("kubernetes/components/volsync/local/externalsecret.yaml");
var replicationSourceTemplate = await GetTemplate("kubernetes/components/volsync/local/replicationsource.yaml");
var pvcTemplate = await GetTemplate("kubernetes/components/volsync/pvc.yaml");
var replicationDestinationTemplate = await GetTemplate("kubernetes/components/volsync/local/replicationdestination.yaml", z =>
{
  if (!replicationSourceTemplate.tokens.TryGetValue("VOLSYNC_CACHE_CAPACITY", out var cacheCapacity)) { return; }
  var digit = int.Parse(string.Join("", cacheCapacity.Where(char.IsDigit)));
  var unit = string.Join("", cacheCapacity.Where(char.IsLetter));
  z["VOLSYNC_CACHE_CAPACITY"] = $"{digit * 4}{unit}";
});

var template = $"""
{pvcTemplate.template}
{secretTemplate.template}
{replicationDestinationTemplate.template}
{replicationSourceTemplate.template}
""";
// dataTemplateName = meta / data

List<string> replicas = [];
for (var i = 0; i < servers; i++)
{
  var replicaName = $"data-garage-{i}";
  var output = ReplaceTokens(template, new Dictionary<string, string>
  {
    ["REPLICA"] = replicaName,
  }
  );
  var fileName = $"replica-{i}-data.yaml";
  File.WriteAllText(Path.Combine(Path.GetDirectoryName(filePath), fileName), output);
  replicas.Add(fileName);
}

var customizationTemplate = $"""
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
{string.Join(Environment.NewLine, replicas.Order().Select(file => $"  - {file}"))}
""";

File.WriteAllText(filePath, customizationTemplate);

AnsiConsole.WriteLine("Replica files created successfully!", new Style(foreground: Color.Green));

async ValueTask<(string template, Dictionary<string, string> tokens)> GetTemplate(string path, Action<Dictionary<string, string>>? mapFunc = null)
{
  var content = await ReadFile(path);
  var template = ReplaceDefaults(content, out var tokens);
  foreach (var item in defaults)
  {
    if (item.Key == "VOLSYNC_CACHE_CAPACITY") throw new InvalidOperationException("VOLSYNC_CACHE_CAPACITY is not supported in this context.");
    tokens[item.Key] = item.Value;
  }
  mapFunc?.Invoke(tokens);
  return (ReplaceTokens(template, tokens), tokens);

}
static string ReplaceTokens(string text, Dictionary<string, string> tokens)
{
  return Regex.Replace(text, @"\$\{(.*?)\}", (b) =>
  {
    var varName = b.Groups[1].Captures[0].Value;
    if (tokens.TryGetValue(varName, out var value))
    {
      // Console.WriteLine($"Replacing variable: {varName} with value: {value}");
      return value;
    }
    return b.Value;
  });
}
static string ReplaceDefaults(string text, out Dictionary<string, string> tokens)
{
  var innerTokens = new Dictionary<string, string>()
  {
    ["APP"] = "${REPLICA}",
  };
  var result = Regex.Replace(text, @"\$\{(.*?)(?:\:\=(.*))?\}", (b) =>
  {
    var varName = b.Groups[1].Captures[0].Value;
    if (b.Groups.Count < 3 || b.Groups[2].Captures.Count == 0)
    {
      return "${" + varName + "}";
    }

    var defaultValue = b.Groups[2]?.Captures[0]?.Value;

    if (!innerTokens.ContainsKey(varName) && defaultValue != null)
    {
      innerTokens[varName] = defaultValue;
    }
    return "${" + varName + "}";
  });

  tokens = innerTokens;
  return result;
}

static async ValueTask<IEnumerable<YamlMappingNode>> ReadStream(string path)
{
  var content = await ReadFile(path);
  content.Dump();
  var doc = new YamlStream();
  using var reader = new StringReader(content);
  doc.Load(reader);


  var rootNodes = doc.Documents
  .Select(z => (z.RootNode as YamlMappingNode)!)
  .Where(z => z is not null);
  return rootNodes;
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
