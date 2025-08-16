using System.Collections.Immutable;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace applications.Models.UptimeKuma;

public class RealBrowserUptime : UptimeBase
{
  public override string Type { get; } = "real-browser";
  public string RemoteBrowser { get; set; }
  public bool? RemoteBrowsersToggle { get; set; }
  public string Url { get; init; }
  [YamlMember(Alias = "accepted_statuscodes")]
  [JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
}
