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

#region Find all applications using maria
try
{

  var kustomizationUserList = new HashSet<string>();
  var users = new Dictionary<string, (string Username, HashSet<(string Name, bool IsPublic)> Buckets)>();
  var documentNamesMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
  string GetName(string keyName)
  {
    if (!documentNamesMapping.TryGetValue(keyName, out var user))
    {
      return keyName.Dump(nameof(keyName));
    }
    return user ?? keyName;
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
    static IReadOnlyList<(string name, string path)> GetComponents(string? path, IEnumerable<YamlNode>? nodes)
    {
      if (path is null) return [];
      return nodes?.OfType<YamlSequenceNode>()
    .SelectMany(z => z.AllNodes.OfType<YamlScalarNode>())
    .Where(z => !string.IsNullOrWhiteSpace(z.Value))
    .Select(z => Path.Combine(path, z.Value))
    .Select(Path.GetFullPath)
    .Select(z => Path.GetRelativePath(Directory.GetCurrentDirectory(), z))
    .Distinct()
    .Select(z => (name: Path.GetFileName(z), path: z))
    .ToList() ?? [];
    }

    ;
    var documentName = kustomizeDoc?.Query("/metadata/name").OfType<YamlScalarNode>().FirstOrDefault()?.Value!;
    documentNamesMapping[documentName] = kustomizeDoc?.Query("/spec/postBuild/substitute/MARIA_NAME")
    .OfType<YamlScalarNode>()
      .SingleOrDefault()
      ?.Value.Dump("MARIA_NAME") ?? documentName;
    var path = kustomizeDoc?.Query("/spec/path").OfType<YamlScalarNode>().FirstOrDefault()?.Value;
    var components = GetComponents(path, kustomizeDoc?.Query("/spec/components"));
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
      try
      {
        ResolveSubComponents(allComponents, component);
      }
      catch (Exception ex)
      {
        continue;
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

  var databases = new List<string>();

  foreach (var item in kustomizeComponents.Where(z => z.Value.Contains("maria")))
  {
    databases.Add(item.Key);
  }

  var serializer = new SerializerBuilder().Build();

  #endregion

  #region Update maria cluster yaml with roles

  var userSecretTemplate = "kubernetes/apps/database/maria/cluster/users/maria-user.yaml";
  // var databaseTemplate = "kubernetes/components/maria/database.yaml";
  // var grantTemplate = "kubernetes/apps/database/maria/cluster/grant.yaml";
  // var userTemplate = "kubernetes/apps/database/maria/cluster/user.yaml";
  var pushSecretTemplate = "kubernetes/apps/database/maria/cluster/push-secret.yaml";
  // We also want to update the kustomization.yaml file to include this user.
  var kustomizationPath = "kubernetes/apps/database/maria/cluster/users/kustomization.yaml";
  var usersDirectory = Path.GetDirectoryName(kustomizationPath)!;

  foreach (var database in databases)
  {
    var roleName = GetName(database);

  }

  #endregion

  #region Create database users

  foreach (var database in databases)
  {
    var roleName = GetName(database);
    var userYaml = File.ReadAllText(userSecretTemplate)
    .Replace("${APP}-user", database)
    .Replace("maria-user-password", $"{roleName}-maria-password")
    .Replace("maria-user", $"{roleName}-maria")
    ;
    // var databaseYaml = File.ReadAllText(databaseTemplate)
    // .Replace("${APP}", database)
    // ;
    var pushSecretYaml = File.ReadAllText(pushSecretTemplate)
    .Replace("${APP}-user", $"{roleName}-maria")
    .Replace("maria-user", $"{roleName}-maria")
    ;
    var fileName = Path.Combine(usersDirectory, $"{roleName}.yaml");
    var sopsFileName = Path.Combine(usersDirectory, $"{roleName}.sops.yaml");
    File.WriteAllText(fileName, $"""
  {userYaml}
  {pushSecretYaml}
  """);
    AnsiConsole.WriteLine($"Updated {fileName} with user {roleName}.");
  }

  foreach (var item in Directory.EnumerateFiles(Path.GetDirectoryName(userSecretTemplate), "*.yaml")
  .Where(z => !z.EndsWith("sops.yaml", StringComparison.OrdinalIgnoreCase))
  .Where(z => !z.EndsWith("kustomization.yaml", StringComparison.OrdinalIgnoreCase))
  )
  {
    var database = GetName(Path.GetFileNameWithoutExtension(item));
    var sopsFileName = Path.Combine(usersDirectory, $"{database}.sops.yaml");
    YamlMappingNode? sopsDoc = null;
    if (File.Exists(sopsFileName))
    {
      continue;
      sopsDoc = ReadStream(sopsFileName).Single();
    }
    File.WriteAllText(sopsFileName, $"""
    # yaml-language-server: $schema=https://kubernetesjsonschema.dev/v1.18.1-standalone-strict/secret-v1.json
    apiVersion: v1
    kind: Secret
    metadata:
      name: {database}-maria-password
    stringData:
      username: "{database}"
      database: "{database}"
      port: "5432"
      hostname: "maria.database.svc.cluster.local"
      password: "{sopsDoc?.Query("/stringData/password").OfType<YamlScalarNode>().SingleOrDefault()?.Value ?? Guid.NewGuid().ToString("N")}"
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

  var customizationTemplate = $"""
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - maria-user.yaml
  - maria-user.sops.yaml
  - maria-superuser.yaml
  - maria-superuser.sops.yaml
{string.Join(Environment.NewLine, databases.Order().Select(GetName).SelectMany(database => new[] { $"  - {database}.yaml", $"  - {database}.sops.yaml" }))}
""";

  File.WriteAllText(kustomizationPath, customizationTemplate);
}
catch (Exception ex)
{
  ex.Dump();
  return;
}

#endregion


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

static YamlMappingNode UpdateRoleNode(ISerializer serializer, YamlNode copy, string name, string key)
{
  var yaml = serializer.Serialize(copy);
  using var reader = new StringReader(yaml);
  var stream = new YamlStream();
  stream.Load(reader);
  var userNode = stream.Documents.First().RootNode as YamlMappingNode;
  var nameRef = userNode.Query("/name").OfType<YamlScalarNode>().Single();
  var superuserRef = userNode.Query("/superuser").OfType<YamlScalarNode>().Single();
  var secretRef = userNode.Query("/passwordSecret").OfType<YamlMappingNode>().Single();
  nameRef.Value = name;
  superuserRef.Value = "false";
  secretRef.Children["name"] = new YamlScalarNode(key);
  return userNode;
}
