using System.Text.Json.Serialization;
using Models.Authentik;
using YamlDotNet.Serialization;

namespace Models.ApplicationDefinition;

public record ApplicationDefinitionAuthentik
{
  [YamlMember(Alias = "saml")]
  [JsonPropertyName("saml")]
  public AuthentikProviderSaml? ProviderSaml { get; init; }

  [YamlMember(Alias = "oidc")]
  [JsonPropertyName("oidc")]
  public AuthentikProviderOauth2? ProviderOauth2 { get; init; }

  [YamlMember(Alias = "oauth2")]
  [JsonPropertyName("oauth2")]
  public AuthentikProviderScim? ProviderScim { get; init; }

  [YamlMember(Alias = "sso")]
  [JsonPropertyName("sso")]
  public AuthentikProviderSsf? ProviderSsf { get; init; }

  [YamlMember(Alias = "proxy")]
  [JsonPropertyName("proxy")]
  public AuthentikProviderProxy? ProviderProxy { get; init; }

  [YamlMember(Alias = "radius")]
  [JsonPropertyName("radius")]
  public AuthentikProviderRadius? ProviderRadius { get; init; }

  [YamlMember(Alias = "rac")]
  [JsonPropertyName("rac")]
  public AuthentikProviderRac? ProviderRac { get; init; }

  [YamlMember(Alias = "ldap")]
  [JsonPropertyName("ldap")]
  public AuthentikProviderLdap? ProviderLdap { get; init; }

  [YamlMember(Alias = "microsoftEntra")]
  [JsonPropertyName("microsoftEntra")]
  public AuthentikProviderMicrosoftEntra? ProviderMicrosoftEntra { get; init; }

  [YamlMember(Alias = "googleWorkspace")]
  [JsonPropertyName("googleWorkspace")]
  public AuthentikProviderGoogleWorkspace? ProviderGoogleWorkspace { get; init; }
}
