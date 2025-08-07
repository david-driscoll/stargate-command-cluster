using System.Collections.Immutable;

namespace authentik.Models;

public class GroupUptime : UptimeBase
{
  public override string Type { get; } = "group";
  [YamlDotNet.Serialization.YamlMember(Alias = "accepted_statuscodes")]
  [System.Text.Json.Serialization.JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
}
