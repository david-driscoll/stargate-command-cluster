#!/usr/bin/dotnet run
#:package YamlDotNet@16.3.0
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

const string TEMPLATE = """
---
# yaml-language-server: $schema=https://raw.githubusercontent.com/datreeio/CRDs-catalog/refs/heads/main/external-secrets.io/externalsecret_v1.json
apiVersion: external-secrets.io/v1
kind: ExternalSecret
metadata:
  name: &name "${DATABASE}-user"
  annotations:
    reloader.stakater.com/auto: "true"
spec:
  refreshInterval: "0"
  target:
    name: *name
    creationPolicy: Owner
    deletionPolicy: Retain
    template:
      metadata:
        labels:
          cnpg.io/reload: "true"
        annotations:
          reloader.stakater.com/auto: "true"
      data:
        username: "${DATABASE}"
        password: "{{ .password }}"
        host: "${APP}-rw.sgc.svc.cluster.local"
        port: "5432"
        database: "${DATABASE}"
        pgpass: "${APP}-rw.sgc.svc.cluster.local:5432:${DATABASE}:${DATABASE}:{{ .password }}"
        jdbc-uri: "jdbc:postgresql://${APP}-rw.sgc.svc.cluster.local:5432/${DATABASE}?password={{ .password }}&user=${DATABASE}"
        uri: "postgresql://${DATABASE}:{{ .password }}@${APP}-rw.sgc.svc.cluster.local:5432/${DATABASE}"
  dataFrom:
    - sourceRef:
        generatorRef:
          apiVersion: generators.external-secrets.io/v1alpha1
          kind: Password
          name: "${APP}-password-generator"
""";


var filePath = "kubernetes/apps/sgc/database/asgard/cluster.yaml";
var databasePath = Path.Combine(Path.GetDirectoryName(filePath), "databases.yaml");

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();
using var input = File.OpenRead(filePath);
using var textReader = new StreamReader(input);
var yaml = new YamlStream();
yaml.Load(textReader);

var databasesContent = new StringBuilder();

var doc = yaml.Documents.First().RootNode as YamlMappingNode;
if (!doc.Children.TryGetValue("spec", out var sn) || sn is not YamlMappingNode specNode) throw new InvalidOperationException("The 'spec' node was not found in the YAML document.");
if (!specNode.Children.TryGetValue("managed", out var mn) || mn is not YamlMappingNode managedNode) throw new InvalidOperationException("The 'managed' node was not found in the YAML document.");
if (!managedNode.Children.TryGetValue("roles", out var ro) || ro is not YamlSequenceNode rolesNode) throw new InvalidOperationException("The 'roles' node was not found in the YAML document.");
foreach (var role in rolesNode.Children)
{
  if (role is not YamlMappingNode roleNode) continue;
  if (!roleNode.Children.TryGetValue("name", out var nameNode) || nameNode is not YamlScalarNode nameScalar) continue;

  AnsiConsole.MarkupLine($"[bold green]Role:[/] {nameScalar.Value}");
  databasesContent.AppendLine(TEMPLATE
      .Replace("${DATABASE}", nameScalar.Value));
}
File.WriteAllText(databasePath, databasesContent.ToString());


AnsiConsole.MarkupLine("[bold green]Database files generated successfully![/]");
