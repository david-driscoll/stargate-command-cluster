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

#region Find all applications using postgres
try
{

  var kustomizationUserList = new HashSet<string>();
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
    documentNamesMapping[documentName] = kustomizeDoc?.Query("/spec/postBuild/substitute/POSTGRES_NAME")
    .OfType<YamlScalarNode>()
      .FirstOrDefault()
      ?.Value.Dump("POSTGRES_NAME") ?? documentName;
    var path = kustomizeDoc?.Query(yamlPath: "/spec/path").OfType<YamlScalarNode>().FirstOrDefault()?.Value;
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
        AnsiConsole.MarkupLine($"[red]Component file {component.path} does not exist for {documentName}.[/]");
        throw new FileNotFoundException($"Component file {component.path} does not exist for {documentName}.");
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

  foreach (var item in kustomizeComponents.Where(z => z.Value.Contains("postgres")))
  {
    databases.Add(item.Key);
  }

  var serializer = new SerializerBuilder().Build();

  #endregion

  #region Update postgres cluster yaml with roles
  var postgresClusterPath = "kubernetes/apps/database/postgres/app/resources/values.yaml";
  var postgresClusterDoc = ReadStream(postgresClusterPath).SingleOrDefault();
  if (postgresClusterDoc == null)
  {
    AnsiConsole.MarkupLine($"[red]Failed to read Postgres cluster file: {postgresClusterPath}.[/]");
    return;
  }
  var clusterRoles = postgresClusterDoc.Query("/cluster/roles").OfType<YamlSequenceNode>().Single();
  var defaultRole = clusterRoles.First();
  clusterRoles.Children.Clear();
  clusterRoles.Children.Add(defaultRole);

  var userTemplate = "kubernetes/apps/database/postgres/app/postgres-user-template.yaml";
  var databaseTemplate = "kubernetes/components/postgres/database.yaml";
  var pushSecretTemplate = "kubernetes/apps/database/postgres/postgres-push-secrets/push-secret-template.yaml";
  // We also want to update the kustomization.yaml file to include this user.
  var kustomizationPath = "kubernetes/apps/database/postgres/app/users/kustomization.yaml";
  var pushSecretKustomizationPath = "kubernetes/apps/database/postgres/postgres-push-secrets/kustomization.yaml";
  var usersDirectory = Path.GetDirectoryName(kustomizationPath)!;
  var pushSecretsDirectory = Path.GetDirectoryName(pushSecretKustomizationPath)!;

  foreach (var database in databases)
  {
    var roleName = GetName(database);
    var roleNode = UpdateRoleNode(serializer, defaultRole, roleName, $"{roleName}-postgres");
    clusterRoles.Children.Add(item: roleNode);

  }

  File.WriteAllText(postgresClusterPath, $"""
---
# yaml-language-server: $schema=https://raw.githubusercontent.com/cloudnative-pg/charts/refs/heads/main/charts/cluster/values.schema.json
{serializer.Serialize(postgresClusterDoc)}
""");

  #endregion

  #region Create database users

  var usersOutputPath = "kubernetes/apps/database/postgres/app/users.yaml";
  var usersOutput = new StringBuilder();
  var pushSecretsOutputPath = "kubernetes/apps/database/postgres/postgres-push-secrets/push-secrets.yaml";
  var pushSecretsOutput = new StringBuilder();
  var sopsOutputPath = "kubernetes/apps/database/postgres/app/passwords.sops.yaml";
  var sopsOutput = new StringBuilder();

  var existingSops =
  ReadStream(sopsOutputPath)
  .Select(z => (name: z.Query("/metadata/name").OfType<YamlScalarNode>().SingleOrDefault()?.Value, node: z))
  .Where(z => z.name is not null)
  .ToDictionary(z => z.name!, z => z.node) ?? new Dictionary<string, YamlMappingNode>();

  foreach (var database in databases)
  {
    var roleName = GetName(database);
    var userYaml = File.ReadAllText(userTemplate)
    .Replace("${APP}-user", database)
    .Replace("postgres-user-password", $"{roleName}-postgres-password")
    .Replace("postgres-user", $"{roleName}-postgres")
    ;
    var databaseYaml = File.ReadAllText(databaseTemplate)
    .Replace("${APP}", database)
    ;
    var pushSecretYaml = File.ReadAllText(pushSecretTemplate)
    .Replace("push-secret-template", $"{roleName}-postgres")
    .Replace("push-secret-template", $"{roleName}-postgres")
    ;
    var fileName = Path.Combine(usersDirectory, $"{roleName}.yaml");
    var pushSecretsFileName = Path.Combine(pushSecretsDirectory, $"{roleName}-postgres-push-secret.yaml");
    usersOutput.AppendLine($"""
  {userYaml}
  {databaseYaml}
  """);
    pushSecretsOutput.AppendLine($"""
  {pushSecretYaml}
  """);
    existingSops.TryGetValue($"{database}-postgres-password", out var existingNode);
    sopsOutput.AppendLine($"""
    ---
    # yaml-language-server: $schema=https://kubernetesjsonschema.dev/v1.18.1-standalone-strict/secret-v1.json
    apiVersion: v1
    kind: Secret
    metadata:
      name: {database}-postgres-password
    stringData:
      username: "{database}"
      database: "{database}"
      port: "5432"
      hostname: "postgres-rw.database.svc.cluster.local"
      password: "{existingNode?.Query("/stringData/password").OfType<YamlScalarNode>().SingleOrDefault()?.Value ?? Guid.NewGuid().ToString("N")}"
    """);
  }

  await Overwrite(usersOutputPath, usersOutput);
  await Overwrite(pushSecretsOutputPath, pushSecretsOutput);
  await Overwrite(sopsOutputPath, sopsOutput);
  await Task.Delay(100);

  Process.Start(new ProcessStartInfo
  {
    FileName = "sops",
    Arguments = $"--encrypt --in-place {sopsOutputPath}",
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true
  })?.WaitForExit();

}
catch (Exception ex)
{
  ex.Dump();
  return;
}

  #endregion


static async Task Overwrite(string path, StringBuilder stringBuilder)
{
  await using var stream = File.Open(path, File.Exists(path) ? FileMode.Truncate : FileMode.Create);
  using var writer = new StreamWriter(stream);
  await writer.WriteAsync(stringBuilder);
}
static IEnumerable<YamlMappingNode> ReadStream(string path)
{
  if (!File.Exists(path))
  {
    AnsiConsole.MarkupLine($"[red]File {path} does not exist.[/]");
    return [];
  }
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
  superuserRef.Value = name == "immich" ? "true" : "false";
  secretRef.Children["name"] = new YamlScalarNode(key);
  return userNode;
}
