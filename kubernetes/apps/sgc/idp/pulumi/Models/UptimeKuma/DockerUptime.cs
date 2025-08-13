using System.Collections.Immutable;

namespace applications.Models.UptimeKuma;

public class DockerUptime : UptimeBase
{
  public override string Type { get; } = "docker";
  [YamlDotNet.Serialization.YamlMember(Alias = "docker_container")]
  [System.Text.Json.Serialization.JsonPropertyName("docker_container")]
  public string DockerContainer { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "docker_host")]
  [System.Text.Json.Serialization.JsonPropertyName("docker_host")]
  public string DockerHost { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "accepted_statuscodes")]
  [System.Text.Json.Serialization.JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
}
