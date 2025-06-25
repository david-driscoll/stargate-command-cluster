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

var serializer = new SerializerBuilder().Build();

#endregion

#region Templates
var minioUsersRelease = "kubernetes/apps/kube-system/minio-users/app/helmrelease.yaml";
var minioUserReleaseMapping = ReadStream(minioUsersRelease);
var containers = minioUserReleaseMapping?.Query("/spec/values/controllers/minio-users/containers").OfType<YamlMappingNode>().Single();
var minioUsersStep = containers.Query("/minio-users").OfType<YamlMappingNode>().Single();

var envReference = minioUsersStep.Query("/env").OfType<YamlMappingNode>().Single();


// TODO tomorrow:

// Update this to update the script
// stamp out each for every user and bucket
// add the correct environment variables


#endregion

var userTemplate = "kubernetes/apps/database/minio/app/cluster-user.yaml";
// We also want to update the kustomization.yaml file to include this user.
var kustomizationPath = "kubernetes/apps/kube-system/minio-users/app/kustomization.yaml";
var helmreleasePath = "kubernetes/apps/kube-system/minio-users/app/helmrelease.yaml";
var usersDirectory = Path.GetDirectoryName(kustomizationPath)!;

var buckets = ImmutableArray.CreateBuilder<string>();
buckets.AddRange(kustomizationUserList);
var users = ImmutableArray.CreateBuilder<string>();
users.AddRange(kustomizationUserList);

var config = "kubernetes/apps/kube-system/minio-users/config.yaml";
if (!File.Exists(config))
{
  File.WriteAllText(config, "");
}
var configDoc = ReadStream(config);
if (configDoc is { })
{
  var _buckets = configDoc.Query("/buckets").OfType<YamlSequenceNode>().ToList();
  var _users = configDoc.Query("/users").OfType<YamlSequenceNode>().ToList();
  buckets.AddRange(_buckets
      .SelectMany(z => z.Children.Dump().OfType<YamlMappingNode>())
      .Select(z => z.Query("/name").OfType<YamlScalarNode>().Single().Value!));
  users.AddRange(_users
  .SelectMany(z => z.Children.Dump().OfType<YamlMappingNode>())
  .Select(z => z.Query("/name").OfType<YamlScalarNode>().Single().Value!));

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

foreach (var user in minioConfig.Users)
{
  var yaml = File.ReadAllText(userTemplate)
  .Replace("cluster-user", $"{user}-minio-access-key")
  .Replace("${APP}", $"minio-users")
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
envReference.Children.Add(new YamlScalarNode($"MINIO_USER_CLUSTER_USER"), GetSecretReference(serializer, envReference["MINIO_ACCESS_KEY"], $"cluster-user", "username"));
envReference.Children.Add(new YamlScalarNode($"MINIO_PASSWORD_CLUSTER_USER"), GetSecretReference(serializer, envReference["MINIO_ACCESS_KEY"], $"cluster-user", "password"));
foreach (var item in minioConfig.Users)
{
  commandBuilder.Add($"mc admin user add \"$MC_ALIAS\" \"$MINIO_USER_{item.ToUpperInvariant()}\" \"$MINIO_PASSWORD_{item.ToUpperInvariant()}\"");
  envReference.Children.Add(new YamlScalarNode($"MINIO_USER_{item.ToUpperInvariant()}"), GetSecretReference(serializer, envReference["MINIO_ACCESS_KEY"], $"{item}-minio-access-key", "username"));
  envReference.Children.Add(new YamlScalarNode($"MINIO_PASSWORD_{item.ToUpperInvariant()}"), GetSecretReference(serializer, envReference["MINIO_ACCESS_KEY"], $"{item}-minio-access-key", "password"));
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
/*
MINIO_SECRET_KEY:
  valueFrom:
    secretKeyRef:
      name: cluster-user
      key: secretkey
*/
minioUsersStep.Children["command"] = new YamlSequenceNode(["/bin/sh", "-c", string.Join("\n", commandBuilder.Select(cmd => cmd))]);

var customizationTemplate = $"""
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - helmrelease.yaml
{string.Join(Environment.NewLine, minioConfig.Users.Select(user => $"  - {user}.yaml"))}
""";

File.WriteAllText(kustomizationPath, customizationTemplate);
File.WriteAllText(helmreleasePath,
"""
---
# yaml-language-server: $schema=https://raw.githubusercontent.com/bjw-s/helm-charts/app-template-3.7.3/charts/other/app-template/schemas/helmrelease-helm-v2.schema.json
""" + "\n" +
serializer.Serialize(minioUserReleaseMapping).Replace("*app:", "*app :"));


static YamlMappingNode? ReadStream(string path)
{
  var doc = new YamlStream();
  using var reader = new StringReader(File.ReadAllText(path));
  doc.Load(reader);

  var rootNode = doc.Documents.FirstOrDefault()?.RootNode as YamlMappingNode;
  return rootNode;
}

record MinioConfig(ImmutableArray<string> Buckets, ImmutableArray<string> Users);
