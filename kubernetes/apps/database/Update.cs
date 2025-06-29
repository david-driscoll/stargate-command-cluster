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
using System.Buffers.Text;
using System.Text.Json;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;

#region Find all applications using minio

var kustomizationUserList = new HashSet<string>();
var kustomizeComponents = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

// Now lets search for all the implied users, and update minio.yaml
foreach (var (kustomizePath, kustomizeDoc) in Directory.EnumerateFiles("kubernetes/apps/", "*.yaml", new EnumerationOptions() { RecurseSubdirectories = true, MatchCasing = MatchCasing.CaseInsensitive })
    .Where(file => file.EndsWith("ks.yaml", StringComparison.OrdinalIgnoreCase))
    .SelectMany(ReadStream, (doc, path) => (doc, path)))
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
    ResolveSubComponents(allComponents, component);
    if (allComponents.Contains("minio-access-key"))
    {
      kustomizationUserList.Add(documentName);
    }
  }
  new { documentName, path, allComponents }.Dump();
  static void ResolveSubComponents(HashSet<string> allComponents, (string name, string path) component)
  {

    var componentDoc = ReadStream(Path.Combine(component.path, "kustomization.yaml")).Single();
    if (componentDoc == null)
    {
      AnsiConsole.MarkupLine($"[yellow]Failed to read kustomization file: {Path.Combine(component.path, "kustomization.yaml")}.[/]");
      return;
    }
    var subComponents = GetComponents(component.path, componentDoc.Query("/components"));

    foreach (var subComponent in subComponents)
    {
      allComponents.Add(subComponent.name);
      ResolveSubComponents(allComponents, subComponent);
    }
  }
  kustomizeComponents[documentName] = allComponents;
}

var serializer = new SerializerBuilder().Build();

#endregion

#region Templates
var minioUsersRelease = "kubernetes/apps/database/minio-users/app/helmrelease.yaml";
var minioUserReleaseMapping = ReadStream(minioUsersRelease)!.Single();
var minioKsYaml = "kubernetes/apps/database/minio-users/ks.yaml";
var minioKsYamlMapping = ReadStream(minioKsYaml)!.Single();
var name = minioUserReleaseMapping.Query("/metadata/name").OfType<YamlScalarNode>().Single().Value;
var controllers = minioUserReleaseMapping.Query($"/spec/values/controllers").OfType<YamlMappingNode>().Single();
var cronController = controllers.Query($"/cron-{name}*").OfType<YamlMappingNode>().Single();
var controller = controllers.Children.Values.Except([cronController]).OfType<YamlMappingNode>().Single(); ;
var containers = controller.Query($"/containers").OfType<YamlMappingNode>().Single();
var minioUsersStep = containers.Query($"/{name}*").OfType<YamlMappingNode>().Single();

var envReference = minioUsersStep.Query("/env").OfType<YamlMappingNode>().Single();


// TODO tomorrow:

// Update this to update the script
// stamp out each for every user and bucket
// add the correct environment variables


#endregion

var userTemplate = "kubernetes/apps/database/minio/app/cluster-user.yaml";
// We also want to update the kustomization.yaml file to include this user.
var kustomizationPath = "kubernetes/apps/database/minio-users/app/kustomization.yaml";
var usersDirectory = Path.GetDirectoryName(kustomizationPath)!;

var buckets = ImmutableArray.CreateBuilder<string>();
buckets.AddRange(kustomizationUserList);
foreach (var item in kustomizeComponents.Where(z => z.Value.Contains("postgres") || z.Value.Contains("postgres-init")))
{
  buckets.Add($"{item.Key}/postgres");
}
foreach (var item in kustomizeComponents.Where(z => z.Value.Contains("mysql")))
{
  buckets.Add($"{item.Key}/mysql/dump");
  buckets.Add($"{item.Key}/mysql/snapshot");
}
var users = ImmutableArray.CreateBuilder<string>();
users.AddRange(kustomizationUserList);

var config = "kubernetes/apps/database/minio-users/config.yaml";
if (!File.Exists(config))
{
  File.WriteAllText(config, "");
}
var configDoc = ReadStream(config).SingleOrDefault();
if (configDoc is { })
{
  var _buckets = configDoc.Query("/buckets").OfType<YamlSequenceNode>().ToList();
  var _users = configDoc.Query("/users").OfType<YamlSequenceNode>().ToList();
  // buckets.AddRange(_buckets
  //     .SelectMany(z => z.Children.Dump().OfType<YamlMappingNode>())
  //     .Select(z => z.Query("/name").OfType<YamlScalarNode>().Single().Value!));
  // users.AddRange(_users
  // .SelectMany(z => z.Children.Dump().OfType<YamlMappingNode>())
  // .Select(z => z.Query("/name").OfType<YamlScalarNode>().Single().Value!));

  // var configUsers = configDoc.Query("/users").OfType<YamlSequenceNode>().ToList().Dump()
  //     .Select(z => z.Query("/name").OfType<YamlScalarNode>().Single().Value)
  //     .ToList();
  // users.AddRange(configUsers);
}

var minioConfig = new MinioConfig(
    Buckets: buckets.ToImmutable(),
    Users: users.ToImmutable()
);
minioConfig.Dump();
var key = "minio-users-" + string.Join("", SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(minioConfig))).Select(z => z.ToString("x2"))).Substring(0, 12);

foreach (var user in minioConfig.Users)
{
  var yaml = File.ReadAllText(userTemplate)
  .Replace("cluster-user", $"{user}-minio-access-key")
  .Replace("${APP}", key)
  .Replace("${CLUSTER_CNAME}", user)
  ;
  var fileName = Path.Combine(usersDirectory, $"{user}.yaml");
  File.WriteAllText(fileName, yaml);
  AnsiConsole.WriteLine($"Updated {fileName} with user {user}.");
}
List<string> commandBuilder = ["mc alias set \"$MC_ALIAS\" \"$MINIO_ENDPOINT\" \"$MINIO_ACCESS_KEY\" \"$MINIO_SECRET_KEY\""];
foreach (var bucket in minioConfig.Buckets)
{
  commandBuilder.Add($"mc mb -p \"$MC_ALIAS/{bucket}\"");
}
envReference.Children.Where(z => z.Key.ToString().StartsWith("MINIO_USER_") || z.Key.ToString().StartsWith("MINIO_PASSWORD_"))
  .ToList()
  .ForEach(z => envReference.Children.Remove(z.Key));

commandBuilder.Add($"mc admin user add \"$MC_ALIAS\" \"$MINIO_USER_CLUSTER_USER\" \"$MINIO_PASSWORD_CLUSTER_USER\"");
commandBuilder.Add($"mc admin policy attach \"$MC_ALIAS\" --user \"$MINIO_USER_CLUSTER_USER\" consoleAdmin");
envReference.Children.Add(new YamlScalarNode($"MINIO_USER_CLUSTER_USER"), GetSecretReference(serializer, envReference["MINIO_ACCESS_KEY"], $"cluster-user", "username"));
envReference.Children.Add(new YamlScalarNode($"MINIO_PASSWORD_CLUSTER_USER"), GetSecretReference(serializer, envReference["MINIO_ACCESS_KEY"], $"cluster-user", "password"));
foreach (var item in minioConfig.Users)
{
  var envKey = item.ToUpperInvariant().Replace("-", "_");
  commandBuilder.Add($"mc admin user add \"$MC_ALIAS\" \"$MINIO_USER_{envKey}\" \"$MINIO_PASSWORD_{envKey}\"");
  commandBuilder.Add($"mc admin policy attach \"$MC_ALIAS\" --user \"$MINIO_USER_{envKey}\" readwrite");
  envReference.Children.Add(new YamlScalarNode($"MINIO_USER_{envKey}"), GetSecretReference(serializer, envReference["MINIO_ACCESS_KEY"], $"{item}-minio-access-key", "username"));
  envReference.Children.Add(new YamlScalarNode($"MINIO_PASSWORD_{envKey}"), GetSecretReference(serializer, envReference["MINIO_ACCESS_KEY"], $"{item}-minio-access-key", "password"));
}
static YamlMappingNode GetSecretReference(ISerializer serializer, YamlNode copy, string name, string key)
{
  var yaml = serializer.Serialize(copy);
  using var reader = new StringReader(yaml);
  var stream = new YamlStream();
  stream.Load(reader);
  var userNode = stream.Documents.First().RootNode as YamlMappingNode;
  var secretRef = userNode.Query("/valueFrom/secretKeyRef").OfType<YamlMappingNode>().Single();
  secretRef.Children["name"] = new YamlScalarNode(name);
  secretRef.Children["key"] = new YamlScalarNode(key);
  return userNode;
}

controllers.Children.Clear();
containers.Children.Clear();
containers.Children[key] = minioUsersStep;
controllers.Children[key] = controller;
controllers.Children[$"cron-{key}"] = cronController;
((YamlScalarNode)((YamlMappingNode)minioUserReleaseMapping.Children["metadata"]).Children["name"]).Value = key;
minioKsYamlMapping.Query("/spec/postBuild/substitute").OfType<YamlMappingNode>().Single()
  .Children["APP"] = new YamlScalarNode(key);
// ((YamlScalarNode)((YamlMappingNode)minioKsYamlMapping.Children["metadata"]).Children["name"]).Value = key;

minioUsersStep.Children["command"] = new YamlSequenceNode(["/bin/sh", "-c", string.Join("\n", commandBuilder.Select(cmd => cmd))]);

var customizationTemplate = $"""
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - helmrelease.yaml
{string.Join(Environment.NewLine, minioConfig.Users.Select(user => $"  - {user}.yaml"))}
""";

File.WriteAllText(kustomizationPath, customizationTemplate);
File.WriteAllText(minioUsersRelease,
"""
---
# yaml-language-server: $schema=https://raw.githubusercontent.com/bjw-s/helm-charts/app-template-3.7.3/charts/other/app-template/schemas/helmrelease-helm-v2.schema.json
""" + "\n" +
serializer.Serialize(minioUserReleaseMapping).Replace("*app:", "*app :"));
File.WriteAllText(minioKsYaml,
"""
---
# yaml-language-server: $schema=https://raw.githubusercontent.com/fluxcd-community/flux2-schemas/main/kustomization-kustomize-v1.json
""" + "\n" +
serializer.Serialize(minioKsYamlMapping).Replace("*app:", "*app :"));


static IEnumerable<YamlMappingNode> ReadStream(string path)
{
  var doc = new YamlStream();
  using var reader = new StringReader(File.ReadAllText(path));
  doc.Load(reader);


  var rootNodes = doc.Documents
  .Select(z => (z.RootNode as YamlMappingNode)!)
  .Where(z => z is not null);
  return rootNodes;
}

record MinioConfig(ImmutableArray<string> Buckets, ImmutableArray<string> Users);
