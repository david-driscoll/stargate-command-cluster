#!/usr/bin/dotnet run
#:package YamlDotNet@16.3.0
#:package gstocco.YamlDotNet.YamlPath@1.0.26
#:package System.Collections.Immutable@10.0.0-preview.5.25277.114
#:package Spectre.Console@0.50.0
#:package Spectre.Console.Json@0.50.0
#:package Dumpify@0.6.6


using System.Buffers.Text;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

#region Find all applications using minio

var kustomizationUserList = new HashSet<string>();
var users = new Dictionary<string, (string Username, HashSet<(string Name, bool IsPublic)> Buckets)>();
var documentNamesMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
string GetName(string keyName)
{
  if (!documentNamesMapping.TryGetValue(keyName, out var user))
  {
    return keyName;
  }
  return user ?? keyName;
}
void AddBucket(string keyName, string bucketName, bool isPublic)
{
  if (!users.TryGetValue(keyName, out var user))
  {
    user = (keyName, []);
    users[keyName] = user;
  }
  user.Buckets.Add((bucketName, isPublic));
}
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

  ;
  var documentName = kustomizeDoc?.Query("/metadata/name").OfType<YamlScalarNode>().FirstOrDefault()?.Value!;
  documentNamesMapping[documentName] = kustomizeDoc?.Query("/spec/postBuild/substitute/APP")
  .OfType<YamlScalarNode>()
    .SingleOrDefault()
    ?.Value ?? documentName;
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
    if (allComponents.Contains("garage-access-key"))
    {
      AddBucket(documentName, documentName, false);
    }
  }
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

foreach (var item in kustomizeComponents.Where(z => z.Value.Contains("postgres") || z.Value.Contains("postgres-init")))
{
  AddBucket(item.Key, $"{GetName(item.Key)}/postgres", false);
}
foreach (var item in kustomizeComponents.Where(z => z.Value.Contains("mysql")))
{
  AddBucket(item.Key, $"{GetName(item.Key)}/mysql/dump", false);
  AddBucket(item.Key, $"{GetName(item.Key)}/mysql/snapshot", false);
}

var config = "kubernetes/apps/database/garage/config.yaml";
if (!File.Exists(config))
{
  File.WriteAllText(config, "");
}
var configDoc = ReadStream(config).SingleOrDefault();
if (configDoc is { })
{
  foreach (var item in configDoc.Query("/users").OfType<YamlSequenceNode>()
  .SelectMany(z => z.Children.OfType<YamlMappingNode>()))
  {
    var name = item.Query("/name").OfType<YamlScalarNode>().Single().Value!;
    foreach (var bucket in item.Query("/buckets").OfType<YamlSequenceNode>()
      .SelectMany(b => b.Children.OfType<YamlMappingNode>()))
    {
      var bucketName = bucket.Query("/name").OfType<YamlScalarNode>().Single().Value!;
      var isPublic = bucket.Query("/public").OfType<YamlScalarNode>().SingleOrDefault()?.Value == "true";
      AddBucket(name, bucketName, isPublic);
    }
  }
}

var serializer = new SerializerBuilder().Build();

#endregion

#region Templates
var minioUsersRelease = "kubernetes/apps/database/garage/app/garage-users.yaml";
var minioUserReleaseMapping = ReadStream(minioUsersRelease)!.Single();
var releaseName = minioUserReleaseMapping.Query("/metadata/name").OfType<YamlScalarNode>().Single().Value;
var controllers = minioUserReleaseMapping.Query($"/spec/values/controllers").OfType<YamlMappingNode>().Single();
var cronController = controllers.Query($"/garage-users-cron").OfType<YamlMappingNode>().Single();
var controller = controllers.Children.Values.Except([cronController]).OfType<YamlMappingNode>().Single(); ;
var containers = controller.Query($"/containers").OfType<YamlMappingNode>().Single();
var minioUsersStep = containers.Query($"/job").OfType<YamlMappingNode>().Single();

var envReference = minioUsersStep.Query("/env").OfType<YamlMappingNode>().Single();


// TODO tomorrow:

// Update this to update the script
// stamp out each for every user and bucket
// add the correct environment variables


#endregion

var userTemplate = "kubernetes/apps/database/garage/app/users/cluster-user.yaml";
// We also want to update the kustomization.yaml file to include this user.
var kustomizationPath = "kubernetes/apps/database/garage/app/users/kustomization.yaml";
var usersDirectory = Path.GetDirectoryName(kustomizationPath)!;

var buckets = ImmutableArray.CreateBuilder<string>();
buckets.AddRange(kustomizationUserList);

var minioConfig = MinioConfig.Create(documentNamesMapping, users);
minioConfig.Dump();

foreach (var user in minioConfig.Users)
{
  var yaml = File.ReadAllText(userTemplate)
  //.Replace("${APP}", key)
  .Replace("${CLUSTER_CNAME}", user.Username)
  .Replace("garage-cluster-user", user.AccessKeyName)
  .Replace("garage-cluster-password", user.PasswordName)
  ;
  var fileName = Path.Combine(usersDirectory, $"{user.Username}.yaml");
  var sopsFileName = Path.Combine(usersDirectory, $"{user.Username}.sops.yaml");
  File.WriteAllText(fileName, yaml);
  AnsiConsole.WriteLine($"Updated {fileName} with user {user.Username}.");

  if (!File.Exists(sopsFileName))
  {
    File.WriteAllText(sopsFileName, $"""
    # yaml-language-server: $schema=https://kubernetesjsonschema.dev/v1.18.1-standalone-strict/secret-v1.json
    apiVersion: v1
    kind: Secret
    metadata:
      name: {user.PasswordName}
    stringData:
      password: "{Guid.NewGuid():N}"
    """);
    Process.Start(new ProcessStartInfo
    {
      FileName = "sops",
      Arguments = $"--encrypt --in-place {sopsFileName}",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    }).WaitForExit();
  }
}
var referenceSecret = envReference["GARAGE_USER_CLUSTER_USER"];
List<string> commandBuilder = [];
envReference.Children.Where(z => z.Key.ToString().StartsWith("GARAGE_USER_") || z.Key.ToString().StartsWith("GARAGE_PASSWORD_"))
  .ToList()
  .ForEach(z => envReference.Children.Remove(z.Key));

commandBuilder.Add($"garage key import -n cluster-user \"$GARAGE_USER_CLUSTER_USER\" \"$GARAGE_PASSWORD_CLUSTER_USER\"");
commandBuilder.Add($"garage key allow --create-bucket cluster-user");
commandBuilder.Add($"garage bucket allow --read --write --owner cluster-user --key cluster-user");
envReference.Children.Add(new YamlScalarNode($"GARAGE_USER_CLUSTER_USER"), GetSecretReference(serializer, referenceSecret, $"garage-cluster-user", "username"));
envReference.Children.Add(new YamlScalarNode($"GARAGE_PASSWORD_CLUSTER_USER"), GetSecretReference(serializer, referenceSecret, $"garage-cluster-user", "password"));
foreach (var user in minioConfig.Users.Order())
{
  var envKey = user.Username.ToUpperInvariant().Replace("-", "_");
  commandBuilder.Add($"garage key import -n {user.Username} \"$GARAGE_USER_{envKey}\" \"$GARAGE_PASSWORD_{envKey}\"");

  foreach (var bucket in user.Buckets.Order())
  {
    commandBuilder.Add($"garage bucket create {bucket.Name}");
    commandBuilder.Add($"garage bucket allow --read --write --owner {bucket.Name} --key {user.Username}");
    if (bucket.IsPublic)
    {
      commandBuilder.Add($"garage bucket website --allow {bucket.Name}");
    }
  }

  envReference.Children.Add(new YamlScalarNode($"GARAGE_USER_{envKey}"), GetSecretReference(serializer, referenceSecret, user.AccessKeyName, "username"));
  envReference.Children.Add(new YamlScalarNode($"GARAGE_PASSWORD_{envKey}"), GetSecretReference(serializer, referenceSecret, user.AccessKeyName, "password"));
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
containers.Children["job"] = minioUsersStep;
controllers.Children["garage-users"] = controller;
controllers.Children[$"garage-users-cron"] = cronController;

minioUsersStep.Children["command"] = new YamlSequenceNode(["/bin/sh", "-c", string.Join("\n", commandBuilder.Select(cmd => cmd))]);

var customizationTemplate = $"""
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - cluster-user.yaml
  - cluster-user.sops.yaml
{string.Join(Environment.NewLine, minioConfig.Users.Order().SelectMany(user => new[] { $"  - {user.Username}.yaml", $"  - {user.Username}.sops.yaml" }))}
""";

File.WriteAllText(kustomizationPath, customizationTemplate);
File.WriteAllText(minioUsersRelease,
"""
---
# yaml-language-server: $schema=https://raw.githubusercontent.com/bjw-s/helm-charts/app-template-4.1.2/charts/other/app-template/schemas/helmrelease-helm-v2.schema.json
""" + "\n" +
serializer.Serialize(minioUserReleaseMapping).Replace("*app:", "*app :"));

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

class MinioConfig
{
  private readonly ImmutableHashSet<(string Username, string AccessKeyName, string PasswordName, ImmutableHashSet<(string Name, bool IsPublic)> Buckets)> _users;

  public static MinioConfig Create(Dictionary<string, string> documentNamesMapping, Dictionary<string, (string Username, HashSet<(string Name, bool IsPublic)> Buckets)> users)
  {
    return new MinioConfig(users.Values
    .Select(user =>
    {
      var username = documentNamesMapping.TryGetValue(user.Username, out var mappedUsername) ? mappedUsername : user.Username;
      return (username, $"{username}-garage-access-key", $"{username}-garage-password", user.Buckets.ToImmutableHashSet());
    })
    );
  }

  private MinioConfig(IEnumerable<(string Username, string AccessKeyName, string PasswordName, ImmutableHashSet<(string Name, bool IsPublic)> Buckets)> users)
  {
    _users = users.ToImmutableHashSet();
  }

  public ImmutableHashSet<(string Username, string AccessKeyName, string PasswordName, ImmutableHashSet<(string Name, bool IsPublic)> Buckets)> Users => _users;

}
