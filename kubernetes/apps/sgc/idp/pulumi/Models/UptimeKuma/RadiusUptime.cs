using System.Collections.Immutable;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Models.UptimeKuma;

public class RadiusUptime : UptimeBase
{
  public override string Type { get; } = "radius";
  [YamlMember(Alias = "hostname")]
  [JsonPropertyName("hostname")]
  public string Hostname { get; set; }
  [YamlMember(Alias = "port")]
  [JsonPropertyName("port")]
  public int? Port { get; init; }
  [YamlMember(Alias = "radius_called_station_id")]
  [JsonPropertyName("radius_called_station_id")]
  public string RadiusCalledStationId { get; init; }
  [YamlMember(Alias = "radius_calling_station_id")]
  [JsonPropertyName("radius_calling_station_id")]
  public string RadiusCallingStationId { get; init; }
  [YamlMember(Alias = "radius_password")]
  [JsonPropertyName("radius_password")]
  public string RadiusPassword { get; init; }
  [YamlMember(Alias = "radius_secret")]
  [JsonPropertyName("radius_secret")]
  public string RadiusSecret { get; init; }
  [YamlMember(Alias = "radius_username")]
  [JsonPropertyName("radius_username")]
  public string RadiusUsername { get; init; }
  [YamlMember(Alias = "accepted_statuscodes")]
  [JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
}
