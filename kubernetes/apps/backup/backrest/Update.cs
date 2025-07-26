#!/usr/bin/dotnet run
#:package YamlDotNet@16.3.0
#:package gstocco.YamlDotNet.YamlPath@1.0.26
#:package System.Collections.Immutable@10.0.0-preview.5.25277.114
#:package Spectre.Console@0.50.0
#:package Spectre.Console.Json@0.50.0
#:package Dumpify@0.6.6
#:package ProcessX@1.5.6
using System.Buffers.Text;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cysharp.Diagnostics;
using Dumpify;
using gfs.YamlDotNet.YamlPath;
using Microsoft.VisualBasic;
using Spectre.Console;
using Spectre.Console.Advanced;
using Spectre.Console.Json;
using YamlDotNet;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#region Find all applications using volsync

var volsyncVolume = new HashSet<string>();
var users = new Dictionary<string, (string Username, HashSet<(string Name, bool IsPublic)> Buckets)>();
var kustomizeComponents = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

// Now lets search for all the implied users, and update minio.yaml
await foreach (var (kustomizePath, kustomizeDoc) in Directory.EnumerateFiles("kubernetes/apps/", "*.yaml", new EnumerationOptions() { RecurseSubdirectories = true, MatchCasing = MatchCasing.CaseInsensitive })
    .Where(file => file.EndsWith("ks.yaml", StringComparison.OrdinalIgnoreCase))
    .ToAsyncEnumerable()
    .SelectMany(z => ReadStream(z), (doc, path) => (doc, path)))
{
  if (kustomizeDoc == null)
  {
    AnsiConsole.MarkupLine($"[yellow]Failed to read kustomization file: {kustomizePath}.[/]");
    continue;
  }
  static IReadOnlyList<(string name, string path)> GetComponents(string path, IEnumerable<YamlNode> nodes)
  {
    return nodes.OfType<YamlSequenceNode>()
  .SelectMany(z => z.AllNodes.OfType<YamlScalarNode>())
  .Select(z => Path.Combine(path, z.Value))
  .Select(Path.GetFullPath)
  .Select(z => Path.GetRelativePath(Directory.GetCurrentDirectory(), z))
  .Distinct()
  .Select(z => (name: Path.GetFileName(z), path: z))
  .ToList();
  }

  ;
  var documentName = kustomizeDoc?.Query("/metadata/name").OfType<YamlScalarNode>().FirstOrDefault()?.Value!;
  var path = kustomizeDoc?.Query("/spec/path").OfType<YamlScalarNode>().FirstOrDefault()?.Value;
  var components = GetComponents(path, kustomizeDoc.Query("/spec/components"));
  if (components.Count == 0)
  {
    // AnsiConsole.MarkupLine($"[green]No components found in {kustomize}.[/]");
    continue;
  }
  var allComponents = new HashSet<string>();
  foreach (var component in components)
  {
    allComponents.Add(component.name);
    // AnsiConsole.MarkupLine($"[blue]Processing component: {component.name}[/]");
    if (!Directory.Exists(component.path))
    {
      AnsiConsole.MarkupLine($"[red]Component file {component.path} does not exist.[/]");
      throw new FileNotFoundException($"Component file {component.path} does not exist.");
    }
    await ResolveSubComponents(allComponents, component);
  }
  static async Task ResolveSubComponents(HashSet<string> allComponents, (string name, string path) component)
  {

    var componentDoc = await ReadStream(Path.Combine(component.path, "kustomization.yaml")).SingleAsync();
    if (componentDoc == null)
    {
      AnsiConsole.MarkupLine($"[yellow]Failed to read kustomization file: {Path.Combine(component.path, "kustomization.yaml")}.[/]");
      return;
    }
    var subComponents = GetComponents(component.path, componentDoc.Query("/components"));

    foreach (var subComponent in subComponents)
    {
      allComponents.Add(subComponent.name);
      await ResolveSubComponents(allComponents, subComponent);
    }
  }
  kustomizeComponents[documentName] = allComponents;
  if (allComponents.Contains("volsync"))
  {
    volsyncVolume.Add(documentName);
  }
}
var serializer = new SerializerBuilder().Build();

var initScripts = new List<string>()
{
  """
  #!/bin/sh

  set -e
  CONFIG_PATH=/app/config.json
  TEMP_CONFIG_PATH=/tmp/config.json.tmp

  if [ ! -f "$CONFIG_PATH" ]; then
    echo "{\"repos\": []}" > $CONFIG_PATH
  fi
  cat $CONFIG_PATH | jq
  jq ".instance = \"${CLUSTER_CNAME}\"" $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
  jq ".version = 4" $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
  jq ".auth.disabled = true" $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
  """,
};
foreach (var volume in volsyncVolume)
{
  initScripts.Add($$$"""
  repo_json=$(jq -n --arg id "{{{volume}}}" --arg uri "/shares/volsync/{{{volume}}}" --arg password "VOLSYNC_PASSWORD" '{id: $id, uri: $uri, password: $password}');
  jq --argjson repo "$repo_json" '.repos += [$repo]' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
  """);
}

initScripts.AddRange([
  """
  # remove any duplicate repos by id
  jq '.repos |= (group_by(.id) | map(.[0]))' $CONFIG_PATH > $TEMP_CONFIG_PATH && cp $TEMP_CONFIG_PATH $CONFIG_PATH
  cat /app/config.json | jq
  """
]);

#endregion

File.WriteAllLines("kubernetes/apps/backup/backrest/app/resources/init.sh", initScripts);

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
