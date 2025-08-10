using System.Collections.Immutable;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Models.UptimeKuma;

public class PushUptime : UptimeBase
{
  public override string Type { get; } = "push";
  [YamlMember(Alias = "push_token")]
  [JsonPropertyName("push_token")]
  public string PushToken { get; set; }
  [YamlMember(Alias = "accepted_statuscodes")]
  [JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
}
