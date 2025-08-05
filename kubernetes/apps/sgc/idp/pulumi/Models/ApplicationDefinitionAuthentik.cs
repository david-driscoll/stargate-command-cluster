using System.Text.Json.Serialization;

namespace authentik.Models;

public record ApplicationDefinitionAuthentik
{
  [JsonPropertyName("saml")] public AuthentikProviderSaml? ProviderSaml { get; init; }
  [JsonPropertyName("oidc")] public AuthentikProviderOauth2? ProviderOauth2 { get; init; }
  [JsonPropertyName("oauth2")] public AuthentikProviderScim? ProviderScim { get; init; }
  [JsonPropertyName("sso")] public AuthentikProviderSsf? ProviderSsf { get; init; }
  [JsonPropertyName("proxy")] public AuthentikProviderProxy? ProviderProxy { get; init; }
  [JsonPropertyName("radius")] public AuthentikProviderRadius? ProviderRadius { get; init; }
  [JsonPropertyName("rac")] public AuthentikProviderRac? ProviderRac { get; init; }
  [JsonPropertyName("ldap")] public AuthentikProviderLdap? ProviderLdap { get; init; }
  [JsonPropertyName("microsoftEntra")] public AuthentikProviderMicrosoftEntra? ProviderMicrosoftEntra { get; init; }
  [JsonPropertyName("googleWorkspace")] public AuthentikProviderGoogleWorkspace? ProviderGoogleWorkspace { get; init; }
}
