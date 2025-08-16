using System.Collections.Immutable;

namespace applications.Models.UptimeKuma;

public class GroupUptime : UptimeBase
{
  public GroupUptime()
  {
    this.Interval = 30;
  }
  public override string Type { get; } = "group";
  [YamlDotNet.Serialization.YamlMember(Alias = "accepted_statuscodes")]
  [System.Text.Json.Serialization.JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
}
