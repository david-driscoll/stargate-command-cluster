#!/usr/bin/dotnet run
#:package YamlDotNet@16.3.0
#:package gstocco.YamlDotNet.YamlPath@1.0.26
#:package System.Collections.Immutable@10.0.0-preview.5.25277.114
#:package Spectre.Console@0.50.0
#:package Spectre.Console.Json@0.50.0
#:package Dumpify@0.6.6


using Spectre.Console;
using Spectre.Console.Json;
using System.Reflection;
using Spectre.Console.Advanced;
using Dumpify;
using YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Immutable;
using gfs.YamlDotNet.YamlPath;
using Microsoft.VisualBasic;
using System.IO.Compression;

#region Find all applications using minio

var kustomizationUserList = new List<string>();

// Now lets search for all the implied users, and update minio.yaml
foreach (var kustomize in Directory.EnumerateFiles("kubernetes/apps/", "*.yaml", new EnumerationOptions() { RecurseSubdirectories = true, MatchCasing = MatchCasing.CaseInsensitive })
    .Where(file => file.EndsWith("ks.yaml", StringComparison.OrdinalIgnoreCase)))
{
  var kustomizeDoc = ReadStream(kustomize);
  if (kustomizeDoc == null)
  {
    AnsiConsole.MarkupLine($"[yellow]Failed to read kustomization file: {kustomize}.[/]");
    continue;
  }
  var documentName = kustomizeDoc?.Query("/metadata/name").OfType<YamlScalarNode>().FirstOrDefault()?.Value!;
  var path = kustomizeDoc?.Query("/spec/path").OfType<YamlScalarNode>().FirstOrDefault()?.Value;
  var components = kustomizeDoc.Query("/spec/components").OfType<YamlSequenceNode>()
  .SelectMany(z => z.AllNodes.OfType<YamlScalarNode>())
  .Select(z => Path.Combine(path, z.Value))
  .Select(Path.GetFullPath)
  .Select(z => Path.GetRelativePath(Directory.GetCurrentDirectory(), z))
  .Distinct()
  .ToList();
  if (components.Count == 0)
  {
    // AnsiConsole.MarkupLine($"[green]No components found in {kustomize}.[/]");
    continue;
  }
  foreach (var component in components)
  {
    var componentName = Path.GetFileName(component);
    // AnsiConsole.MarkupLine($"[blue]Processing component: {componentName}[/]");
    if (!Directory.Exists(component))
    {
      AnsiConsole.MarkupLine($"[red]Component file {component} does not exist.[/]");
      throw new FileNotFoundException($"Component file {component} does not exist.");
    }

    new { component, componentName, documentName }.Dump();

    if (componentName == "minio-access-key".Trim('/', '\\'))
    {
      kustomizationUserList.Add(documentName);
      break;
    }
    var componentDoc = ReadStream(Path.Combine(component, "kustomization.yaml"));
    if (componentDoc == null)
    {
      AnsiConsole.MarkupLine($"[yellow]Failed to read kustomization file: {Path.Combine(component, "kustomization.yaml")}.[/]");
      continue;
    }
    var subComponents = componentDoc.Query("/components").OfType<YamlSequenceNode>()
      .SelectMany(z => z.AllNodes.OfType<YamlScalarNode>())
      .Select(z => Path.Combine(path, z.Value))
      .Select(Path.GetFullPath)
      .Select(z => Path.GetRelativePath(Directory.GetCurrentDirectory(), z))
      .Distinct()
      .Select(z => new { subComponentName = Path.GetFileName(z), subComponentPath = z })
      .ToList();

    // new { component, componentName, subComponents }.Dump();
    if (subComponents
      .Any(z => z.subComponentName == "minio-access-key".Trim('/', '\\')))
    {
      kustomizationUserList.Add(documentName);
      break;
    }
  }
}

#endregion


var valuesTemplate = "kubernetes/apps/database/minio/app/values.yaml";
var configPath = "kubernetes/apps/kube-system/minio-users/minio.yaml";
var userTemplate = "kubernetes/apps/database/minio/app/cluster-user.yaml";
// We also want to update the kustomization.yaml file to include this user.
var kustomizationPath = "kubernetes/apps/kube-system/minio-users/app/kustomization.yaml";
var usersDirectory = Path.GetDirectoryName(kustomizationPath)!;

var buckets = ImmutableArray.CreateBuilder<string>();
var users = ImmutableArray.CreateBuilder<string>();
var minioConfig = new MinioConfig(
    Buckets: buckets.ToImmutable(),
    Users: users.ToImmutable()
);
minioConfig.Dump();

var missingUsers = kustomizationUserList.Except(minioConfig.Users).ToList();
var missingBuckets = kustomizationUserList.Except(minioConfig.Buckets).ToList();
missingUsers.Dump(label: "Missing Users");
if (missingUsers.Count > 0 || missingBuckets.Count > 0)
{
  minioConfig = new MinioConfig(
      minioConfig.Buckets.AddRange(missingBuckets).ToImmutableArray(),
      minioConfig.Users.AddRange(missingUsers).ToImmutableArray()
  );
}

foreach (var item in Directory.EnumerateFiles(usersDirectory, "*.yaml"))
{
  File.Delete(item);
}

foreach (var user in minioConfig.Users)
{
  var yaml = File.ReadAllText(userTemplate)
  .Replace("cluster-user", $"{user}-minio-access-key")
  .Replace("${APP}", $"minio-users")
  ;
  var fileName = Path.Combine(usersDirectory, $"{user}.yaml");
  File.WriteAllText(fileName, yaml);
  AnsiConsole.WriteLine($"Updated {fileName} with user {user}.");
}

var customizationTemplate = $"""
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
configMapGenerator:
  - name: minio-values
    files:
      - values.yaml=values.yaml
    options:
      disableNameSuffixHash: true
resources:
{string.Join(Environment.NewLine, minioConfig.Users.Select(user => $"  - {user}.yaml"))}
""";

File.WriteAllText(kustomizationPath, customizationTemplate);

File.WriteAllText(Path.Combine(Path.GetDirectoryName(kustomizationPath), "values.yaml"), File.ReadAllText(valuesTemplate)
  .Replace("${MINIO_BUCKETS}", string.Join("\n    ", minioConfig.Buckets.Select(user => $"- name: {user}")))
  .Replace("${MINIO_USERS}", string.Join("\n    ", minioConfig.Users.Prepend("cluster-user").Select(bucket => $"- {bucket}"))));

static YamlMappingNode? ReadStream(string path)
{
  var doc = new YamlStream();
  using var reader = new StringReader(File.ReadAllText(path));
  doc.Load(reader);

  var rootNode = doc.Documents.FirstOrDefault()?.RootNode as YamlMappingNode;
  return rootNode;
}

record MinioConfig(ImmutableArray<string> Buckets, ImmutableArray<string> Users);
