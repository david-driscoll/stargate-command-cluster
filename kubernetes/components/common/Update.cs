#!/usr/bin/dotnet run
#:package YamlDotNet@16.3.0
#:package gstocco.YamlDotNet.YamlPath@1.0.26
#:package Spectre.Console@0.50.0
#:package Spectre.Console.Json@0.50.0
#:package Dumpify@0.6.6
#:package System.Collections.Immutable@10.0.0-preview.5.25277.114


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

    // new { component, componentName, documentName }.Dump();

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

var configPath = "kubernetes/components/common/minio.yaml";
var userTemplate = "kubernetes/apps/database/minio/app/cluster-user.yaml";
// We also want to update the kustomization.yaml file to include this user.
var kustomizationPath = "kubernetes/apps/kube-system/minio-users/app/kustomization.yaml";
var usersDirectory = Path.GetDirectoryName(kustomizationPath)!;
const string customizationTemmplate = """
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
""";

var configStream = ReadStream(configPath);
var buckets = ImmutableArray.CreateBuilder<string>();
var users = ImmutableArray.CreateBuilder<string>();
if (configStream == null)
{
  AnsiConsole.MarkupLine($"[red]Failed to read minio configuration file: {configPath}.[/]");
}
else
{
  foreach (var child in configStream.Children)
  {
    if (child.Key is YamlScalarNode keyNode && keyNode.Value == "MINIO_BUCKETS" && child.Value is YamlSequenceNode bucketsNode)
    {
      foreach (var bucket in bucketsNode.Children.OfType<YamlMappingNode>())
      {
        buckets.Add(bucket["name"].ToString());
      }
    }
    else if (child.Key is YamlScalarNode userKeyNode && userKeyNode.Value == "MINIO_USERS" && child.Value is YamlSequenceNode usersNode)
    {
      foreach (var user in usersNode.Children.OfType<YamlScalarNode>())
      {
        users.Add(user.Value);
      }

    }
  }
}

var minioConfig = new MinioConfig(
    Buckets: buckets.ToImmutable(),
    Users: users.ToImmutable().Remove("cluster-user")
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
  File.WriteAllText(configPath, $"""
  MINIO_BUCKETS:
  {string.Join(Environment.NewLine, minioConfig.Buckets.Select(bucket => $"- name: {bucket}"))}
  MINIO_USERS:
  - cluster-user
  {string.Join(Environment.NewLine, minioConfig.Users.Select(user => $"- {user}"))}
  """);
}

foreach (var item in Directory.EnumerateFiles(usersDirectory, "*.yaml"))
{
  File.Delete(item);
}

foreach (var user in minioConfig.Users)
{
  var yaml = File.ReadAllText(userTemplate).Replace("cluster-user", $"{user}-minio-access-key");
  var fileName = Path.Combine(usersDirectory, $"{user}.yaml");
  File.WriteAllText(fileName, yaml);
  AnsiConsole.WriteLine($"Updated {fileName} with user {user}.");
}

File.WriteAllText(kustomizationPath, customizationTemmplate + Environment.NewLine + minioConfig.Users
    .Select(user => $"- ./{user}.yaml")
    .Aggregate((current, next) => current + Environment.NewLine + next));

static YamlMappingNode? ReadStream(string path)
{
  var doc = new YamlStream();
  using var reader = new StringReader(File.ReadAllText(path));
  doc.Load(reader);

  var rootNode = doc.Documents.FirstOrDefault()?.RootNode as YamlMappingNode;
  return rootNode;
}

record MinioConfig(ImmutableArray<string> Buckets, ImmutableArray<string> Users);
