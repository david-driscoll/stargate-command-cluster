#!/usr/bin/dotnet run
#:package YamlDotNet@16.3.0
#:package gstocco.YamlDotNet.YamlPath@1.0.26
#:package System.Collections.Immutable@10.0.0-preview.5.25277.114
#:package Spectre.Console@0.50.0
#:package Spectre.Console.Json@0.50.0
#:package Dumpify@0.6.6

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

var template = """
---
# yaml-language-server: $schema=https://raw.githubusercontent.com/datreeio/CRDs-catalog/refs/heads/main/kustomize.toolkit.fluxcd.io/kustomization_v1.json
apiVersion: kustomize.toolkit.fluxcd.io/v1
kind: Kustomization
metadata:
  name: &app secret-store
  namespace: &namespace ${NAMESPACE}
spec:
  commonMetadata:
    labels:
      app.kubernetes.io/name: *app
  dependsOn:
    - name: external-secrets-stores
      namespace: kube-system
  path: ./kubernetes/flux/secret-store
  prune: true
  wait: true
  force: false # true
  interval: 1h
  timeout: 5m
  sourceRef:
    kind: GitRepository
    name: flux-system
    namespace: flux-system
  targetNamespace: *namespace
  postBuild:
    substitute:
      APP: *app
      NAMESPACE: *namespace
""";

foreach (var item in Directory.EnumerateDirectories("kubernetes/apps"))
{
  item.Dump();
  File.WriteAllText(Path.Combine(item, "secret-store.yaml"), template.Replace("${NAMESPACE}", Path.GetFileName(item)));
}
