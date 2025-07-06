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
using gfs.YamlDotNet.YamlPath;

var template = """
---
# yaml-language-server: $schema=https://homelab-schemas-epg.pages.dev/external-secrets.io/clustersecretstore_v1.json
apiVersion: external-secrets.io/v1
kind: SecretStore
metadata:
  name: namespace
spec:
  provider:
    kubernetes:
      remoteNamespace: ${NAMESPACE}
      server:
        caProvider:
          type: ConfigMap
          name: kube-root-ca.crt
          key: ca.crt
      auth:
        serviceAccount:
          name: "external-secrets"
          namespace: kube-system
""";

foreach (var item in Directory.EnumerateDirectories("kubernetes/apps"))
{
  item.Dump();
  File.WriteAllText(Path.Combine(item, "secret-store.yaml"), template.Replace("${NAMESPACE}", Path.GetFileName(item)));
}
