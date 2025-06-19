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
using System.Text.RegularExpressions;

Dictionary<string, string> defaults = new Dictionary<string, string>()
{
  // { "VOLSYNC_CAPACITY", "" },
  // { "VOLSYNC_CACHE_CAPACITY", "" },
  // { "VOLSYNC_STORAGECLASS", "" },
  // { "VOLSYNC_COPYMETHOD", "" },
  // { "VOLSYNC_SNAPSHOTCLASS", "" },
  // { "VOLSYNC_CACHE_ACCESSMODES", "ReadWriteOnce" },
  // { "VOLSYNC_CACHE_SNAPSHOTCLASS", "" },
  // { "VOLSYNC_PUID", 568 },
  // { "VOLSYNC_PGID", 568 },

};

var filePath = "kubernetes/apps/sgc/dns/adguard-home/helmrelease.yaml";

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();
using var input = File.OpenRead(filePath);
using var textReader = new StreamReader(input);
var yaml = new YamlStream();
yaml.Load(textReader);

var databasesContent = new StringBuilder();

int? replicas = null;
string? templateName = null;
var doc = yaml.Documents.First().RootNode as YamlMappingNode;
foreach (var node in doc.AllNodes.OfType<YamlMappingNode>())
{
  if (node.Children.TryGetValue("replicas", out var child) && child is YamlScalarNode replicasNode && int.TryParse(replicasNode.Value, out int replicasValue))
  {
    replicas = replicasValue;
  }
}
foreach (var node in doc.AllNodes.OfType<YamlMappingNode>())
{
  if (node.Children.TryGetValue("volumeClaimTemplates", out var child) && child is YamlSequenceNode volumeClaimTemplatesNode)
  {
    var first = volumeClaimTemplatesNode.Children.OfType<YamlMappingNode>().First();
    if (!first.Children.TryGetValue("name", out var volumeNameNode) || volumeNameNode is not YamlScalarNode volumeNameScalar)
    {
      throw new InvalidOperationException("The 'name' node was not found in the YAML document.");
    }
    if (first.Children.TryGetValue("size", out var volumeSizeNode) && volumeSizeNode is YamlScalarNode { Value: { Length: > 0 } } volumeSizeScalar)
    {
      defaults["VOLSYNC_CAPACITY"] = volumeSizeScalar.Value;
    }
    if (first.Children.TryGetValue("accessMode", out var volumeAccessModeNode) && volumeAccessModeNode is YamlScalarNode { Value: { Length: > 0 } } volumeAccessModeScalar)
    {
      defaults["VOLSYNC_ACCESS_MODE"] = volumeAccessModeScalar.Value;
    }
    templateName = volumeNameScalar.Value;
  }
}
if (!doc.Children.TryGetValue("metadata", out var sn) || sn is not YamlMappingNode metadataNode) throw new InvalidOperationException("The 'metadata' node was not found in the YAML document.");
if (!metadataNode.Children.TryGetValue("name", out var mn) || mn is not YamlScalarNode nameNode) throw new InvalidOperationException("The 'name' node was not found in the YAML document.");
var app = nameNode.Value;


var secretTemplate = GetTemplate("kubernetes/components/volsync/local/externalsecret.yaml");
var replicationSourceTemplate = GetTemplate("kubernetes/components/volsync/local/replicationsource.yaml");
var pvcTemplate = GetTemplate("kubernetes/components/volsync/pvc.yaml");
var destinationTemplate = GetTemplate("kubernetes/components/volsync/local/replicationdestination.yaml", z =>
{
  if (!z.TryGetValue("VOLSYNC_CACHE_CAPACITY", out var cacheCapacity)) { return; }
  if (!defaults.TryGetValue("VOLSYNC_CAPACITY", out var capacity)) { return; }
  var digit = int.Parse(string.Join("", capacity.Where(char.IsDigit)));
  var unit = string.Join("", capacity.Where(char.IsLetter));
  z["VOLSYNC_CACHE_CAPACITY"] = $"{digit * 4}{unit}";
});

var template = $"""
{pvcTemplate}
{secretTemplate}
{destinationTemplate}
{replicationSourceTemplate}
""";

for (var i = 0; i < replicas; i++)
{
  var replicaName = $"{templateName}-{app}-{i}";
  var output = ReplaceTokens(template, new Dictionary<string, string>
  {
    ["REPLICA"] = replicaName,
  }
  );
  File.WriteAllText($"kubernetes/apps/sgc/dns/adguard-home/replica-{i}.yaml", output);
}

AnsiConsole.WriteLine("Replica files created successfully!", new Style(foreground: Color.Green));

string GetTemplate(string path, Action<Dictionary<string, string>>? mapFunc = null)
{
  var template = ReplaceDefaults(File.ReadAllText(path), out var tokens);
  foreach (var item in defaults)
  {
    if (tokens.ContainsKey(item.Key)) continue;
    tokens[item.Key] = item.Value;
  }
  mapFunc?.Invoke(tokens);
  return ReplaceTokens(template, tokens);

}
static string ReplaceTokens(string text, Dictionary<string, string> tokens)
{
  return Regex.Replace(text, @"\$\{(.*?)\}", (b) =>
  {
    var varName = b.Groups[1].Captures[0].Value;
    if (tokens.TryGetValue(varName, out var value))
    {
      // AnsiConsole.WriteLine($"Replacing variable: {varName} with value: {value}", new Style(foreground: Color.Cyan1));
      return value;
    }
    return b.Value;
  });
}
static string ReplaceDefaults(string text, out Dictionary<string, string> tokens)
{
  var innerTokens = new Dictionary<string, string>()
  {
    ["APP"] = "${REPLICA}",
  };
  var result = Regex.Replace(text, @"\$\{(.*?)(?:\:\=(.*))?\}", (b) =>
  {
    var varName = b.Groups[1].Captures[0].Value;
    if (b.Groups.Count < 3 || b.Groups[2].Captures.Count == 0)
    {
      return "${" + varName + "}";
    }

    var defaultValue = b.Groups[2]?.Captures[0]?.Value;

    if (!innerTokens.ContainsKey(varName) && defaultValue != null)
    {
      innerTokens[varName] = defaultValue;
    }
    return "${" + varName + "}";
  });

  tokens = innerTokens;
  return result;
}
