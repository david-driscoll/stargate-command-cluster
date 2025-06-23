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

#region Templates
var minioUsersRelease = "kubernetes/apps/kube-system/minio-users/app/helmrelease.yaml";
var minioUserReleaseMapping = ReadStream(minioUsersRelease);
var addBucketsTemplate = minioUserReleaseMapping.Query("/spec/values/controllers/add-buckets").FirstOrDefault();
var addUsersTemplate = minioUserReleaseMapping.Query("/spec/values/controllers/add-users").FirstOrDefault();

// TODO tomorrow:

// Update this to update the script
// stamp out each for every user and bucket
// add the correct environment variables

var creationTemplate = """
---
# yaml-language-server: $schema=https://raw.githubusercontent.com/bjw-s/helm-charts/app-template-3.7.3/charts/other/app-template/schemas/helmrelease-helm-v2.schema.json
apiVersion: helm.toolkit.fluxcd.io/v2
kind: HelmRelease
metadata:
  name: &app minio-users
spec:
  timeout: 5m
  interval: 30m
  chartRef:
    kind: OCIRepository
    name: app-template
  install:
    remediation:
      retries: 3
    replace: true
  upgrade:
    force: true
    cleanupOnFail: true
    remediation:
      retries: 3
  uninstall:
    keepHistory: false
  values:
    defaultPodOptions:
      securityContext:
        fsGroup: 568
        fsGroupChangePolicy: "OnRootMismatch"
        runAsGroup: 568
        runAsNonRoot: true
        runAsUser: 568
        seccompProfile:
          type: RuntimeDefault
      shareProcessNamespace: true
    controllers:
      add-buckets:
{buckets}
      add-users:
{users}

    persistence:
      tmp:
        type: emptyDir
      resources:
        type: configMap
        name: minio-scripts
        defaultMode: 493
""";

var bucketsTemplate = """
        type: job
        job:
          backoffLimit: 6
          suspend: false
        containers:
          main: &job
            image:
              repository: minio/mc
              tag: RELEASE.2025-05-21T01-59-54Z.hotfix.e98f1ead@sha256:cf700affaa5cddcea9371fd4c961521fff2baff4b90333c4bda2df61bf5e6692
              pullPolicy: IfNotPresent
            command:
              - sh
              - -c
              - |
                mc alias set "$MC_ALIAS" "$MINIO_ENDPOINT" "$MINIO_ACCESS_KEY" "$MINIO_SECRET_KEY"

                mc mb -p "$MC_ALIAS/$MINIO_BUCKET"

                mc admin user add
            env:
              MC_ALIAS: ${CLUSTER_CNAME}
              MINIO_ENDPOINT: http://minio.database.${INTERNAL_CLUSTER_DOMAIN}:9000
              MINIO_ACCESS_KEY:
                valueFrom:
                  secretKeyRef:
                    name: cluster-user
                    key: accesskey
              MINIO_SECRET_KEY:
                valueFrom:
                  secretKeyRef:
                    name: cluster-user
                    key: secretkey
              MC_CONFIG_DIR: /tmp/.mc
              PUID: 568
              PGID: 568
              UMASK: 002
              TZ: "${TIMEZONE}"
            envFrom:
              - secretRef:
                  name: ${APP}-minio
            resources:
              requests:
                cpu: 10m
                memory: 32Mi
""";
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
  .Replace("${CLUSTER_CNAME}", user)
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
