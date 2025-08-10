using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Models.Authentik;

public record AuthentikSpec
{
  [JsonPropertyName("type")]
  [YamlMember(Alias = "type")]
  public string? Type { get; set; }
  [JsonPropertyName("eventRetention")]
  [YamlMember(Alias = "eventRetention")]
  public string? EventRetention { get; set; }

  [JsonPropertyName("jwtFederationProviders")]
  [YamlMember(Alias = "jwtFederationProviders")]
  public string? JwtFederationProviders { get; set; }

  [JsonPropertyName("providerSsfId")]
  [YamlMember(Alias = "providerSsfId")]
  public string? ProviderSsfId { get; set; }
  [JsonPropertyName("signingKey")]
  [YamlMember(Alias = "signingKey")]
  public string? SigningKey { get; set; }

  [JsonPropertyName("compatibilityMode")]
  [YamlMember(Alias = "compatibilityMode")]
  public string? CompatibilityMode { get; set; }

  [JsonPropertyName("dryRun")]
  [YamlMember(Alias = "dryRun")]
  public string? DryRun { get; set; }

  [JsonPropertyName("excludeUsersServiceAccount")]
  [YamlMember(Alias = "excludeUsersServiceAccount")]
  public string? ExcludeUsersServiceAccount { get; set; }

  [JsonPropertyName("filterGroup")]
  [YamlMember(Alias = "filterGroup")]
  public string? FilterGroup { get; set; }
  [JsonPropertyName("propertyMappings")]
  [YamlMember(Alias = "propertyMappings")]
  public string? PropertyMappings { get; set; }

  [JsonPropertyName("propertyMappingsGroups")]
  [YamlMember(Alias = "propertyMappingsGroups")]
  public string? PropertyMappingsGroups { get; set; }

  [JsonPropertyName("providerScimId")]
  [YamlMember(Alias = "providerScimId")]
  public string? ProviderScimId { get; set; }
  [JsonPropertyName("token")]
  [YamlMember(Alias = "token")]
  public string? Token { get; set; }
  [JsonPropertyName("url")]
  [YamlMember(Alias = "url")]
  public string? Url { get; set; }
  [JsonPropertyName("acsUrl")]
  [YamlMember(Alias = "acsUrl")]
  public string? AcsUrl { get; set; }

  [JsonPropertyName("assertionValidNotBefore")]
  [YamlMember(Alias = "assertionValidNotBefore")]
  public string? AssertionValidNotBefore { get; set; }

  [JsonPropertyName("assertionValidNotOnOrAfter")]
  [YamlMember(Alias = "assertionValidNotOnOrAfter")]
  public string? AssertionValidNotOnOrAfter { get; set; }

  [JsonPropertyName("audience")]
  [YamlMember(Alias = "audience")]
  public string? Audience { get; set; }

  [JsonPropertyName("authenticationFlow")]
  [YamlMember(Alias = "authenticationFlow")]
  public string? AuthenticationFlow { get; set; }

  [JsonPropertyName("authnContextClassRefMapping")]
  [YamlMember(Alias = "authnContextClassRefMapping")]
  public string? AuthnContextClassRefMapping { get; set; }

  [JsonPropertyName("authorizationFlow")]
  [YamlMember(Alias = "authorizationFlow")]
  public string? AuthorizationFlow { get; set; }

  [JsonPropertyName("defaultRelayState")]
  [YamlMember(Alias = "defaultRelayState")]
  public string? DefaultRelayState { get; set; }

  [JsonPropertyName("digestAlgorithm")]
  [YamlMember(Alias = "digestAlgorithm")]
  public string? DigestAlgorithm { get; set; }
  [JsonPropertyName("encryptionKp")]
  [YamlMember(Alias = "encryptionKp")]
  public string? EncryptionKp { get; set; }
  [JsonPropertyName("invalidationFlow")]
  [YamlMember(Alias = "invalidationFlow")]
  public string? InvalidationFlow { get; set; }
  [JsonPropertyName("issuer")]
  [YamlMember(Alias = "issuer")]
  public string? Issuer { get; set; }
  [JsonPropertyName("nameIdMapping")]
  [YamlMember(Alias = "nameIdMapping")]
  public string? NameIdMapping { get; set; }
  [JsonPropertyName("providerSamlId")]
  [YamlMember(Alias = "providerSamlId")]
  public string? ProviderSamlId { get; set; }

  [JsonPropertyName("sessionValidNotOnOrAfter")]
  [YamlMember(Alias = "sessionValidNotOnOrAfter")]
  public string? SessionValidNotOnOrAfter { get; set; }

  [JsonPropertyName("signAssertion")]
  [YamlMember(Alias = "signAssertion")]
  public string? SignAssertion { get; set; }
  [JsonPropertyName("signResponse")]
  [YamlMember(Alias = "signResponse")]
  public string? SignResponse { get; set; }

  [JsonPropertyName("signatureAlgorithm")]
  [YamlMember(Alias = "signatureAlgorithm")]
  public string? SignatureAlgorithm { get; set; }

  [JsonPropertyName("signingKp")]
  [YamlMember(Alias = "signingKp")]
  public string? SigningKp { get; set; }
  [JsonPropertyName("spBinding")]
  [YamlMember(Alias = "spBinding")]
  public string? SpBinding { get; set; }
  [JsonPropertyName("urlSloPost")]
  [YamlMember(Alias = "urlSloPost")]
  public string? UrlSloPost { get; set; }
  [JsonPropertyName("urlSloRedirect")]
  [YamlMember(Alias = "urlSloRedirect")]
  public string? UrlSloRedirect { get; set; }
  [JsonPropertyName("urlSsoInit")]
  [YamlMember(Alias = "urlSsoInit")]
  public string? UrlSsoInit { get; set; }
  [JsonPropertyName("urlSsoPost")]
  [YamlMember(Alias = "urlSsoPost")]
  public string? UrlSsoPost { get; set; }
  [JsonPropertyName("urlSsoRedirect")]
  [YamlMember(Alias = "urlSsoRedirect")]
  public string? UrlSsoRedirect { get; set; }
  [JsonPropertyName("verificationKp")]
  [YamlMember(Alias = "verificationKp")]
  public string? VerificationKp { get; set; }

  [JsonPropertyName("accessTokenValidity")]
  [YamlMember(Alias = "accessTokenValidity")]
  public string? AccessTokenValidity { get; set; }

  [JsonPropertyName("basicAuthEnabled")]
  [YamlMember(Alias = "basicAuthEnabled")]
  public string? BasicAuthEnabled { get; set; }

  [JsonPropertyName("basicAuthPasswordAttribute")]
  [YamlMember(Alias = "basicAuthPasswordAttribute")]
  public string? BasicAuthPasswordAttribute { get; set; }

  [JsonPropertyName("basicAuthUsernameAttribute")]
  [YamlMember(Alias = "basicAuthUsernameAttribute")]
  public string? BasicAuthUsernameAttribute { get; set; }

  [JsonPropertyName("cookieDomain")]
  [YamlMember(Alias = "cookieDomain")]
  public string? CookieDomain { get; set; }
  [JsonPropertyName("externalHost")]
  [YamlMember(Alias = "externalHost")]
  public string? ExternalHost { get; set; }

  [JsonPropertyName("interceptHeaderAuth")]
  [YamlMember(Alias = "interceptHeaderAuth")]
  public string? InterceptHeaderAuth { get; set; }

  [JsonPropertyName("internalHost")]
  [YamlMember(Alias = "internalHost")]
  public string? InternalHost { get; set; }

  [JsonPropertyName("internalHostSslValidation")]
  [YamlMember(Alias = "internalHostSslValidation")]
  public string? InternalHostSslValidation { get; set; }

  [JsonPropertyName("jwksSources")]
  [YamlMember(Alias = "jwksSources")]
  public string? JwksSources { get; set; }

  [JsonPropertyName("jwtFederationSources")]
  [YamlMember(Alias = "jwtFederationSources")]
  public string? JwtFederationSources { get; set; }

  [JsonPropertyName("mode")]
  [YamlMember(Alias = "mode")]
  public string? Mode { get; set; }
  [JsonPropertyName("providerProxyId")]
  [YamlMember(Alias = "providerProxyId")]
  public string? ProviderProxyId { get; set; }

  [JsonPropertyName("refreshTokenValidity")]
  [YamlMember(Alias = "refreshTokenValidity")]
  public string? RefreshTokenValidity { get; set; }

  [JsonPropertyName("skipPathRegex")]
  [YamlMember(Alias = "skipPathRegex")]
  public string? SkipPathRegex { get; set; }
  [JsonPropertyName("clientNetworks")]
  [YamlMember(Alias = "clientNetworks")]
  public string? ClientNetworks { get; set; }
  [JsonPropertyName("mfaSupport")]
  [YamlMember(Alias = "mfaSupport")]
  public string? MfaSupport { get; set; }
  [JsonPropertyName("providerRadiusId")]
  [YamlMember(Alias = "providerRadiusId")]
  public string? ProviderRadiusId { get; set; }
  [JsonPropertyName("sharedSecret")]
  [YamlMember(Alias = "sharedSecret")]
  public string? SharedSecret { get; set; }
  [JsonPropertyName("connectionExpiry")]
  [YamlMember(Alias = "connectionExpiry")]
  public string? ConnectionExpiry { get; set; }
  [JsonPropertyName("providerRacId")]
  [YamlMember(Alias = "providerRacId")]
  public string? ProviderRacId { get; set; }
  [JsonPropertyName("settings")]
  [YamlMember(Alias = "settings")]
  public string? Settings { get; set; }

  [JsonPropertyName("accessCodeValidity")]
  [YamlMember(Alias = "accessCodeValidity")]
  public string? AccessCodeValidity { get; set; }

  [JsonPropertyName("allowedRedirectUris")]
  [YamlMember(Alias = "allowedRedirectUris")]
  public string? AllowedRedirectUris { get; set; }

  [JsonPropertyName("clientId")]
  [YamlMember(Alias = "clientId")]
  public string? ClientId { get; set; }
  [JsonPropertyName("clientSecret")]
  [YamlMember(Alias = "clientSecret")]
  public string? ClientSecret { get; set; }
  [JsonPropertyName("clientType")]
  [YamlMember(Alias = "clientType")]
  public string? ClientType { get; set; }
  [JsonPropertyName("encryptionKey")]
  [YamlMember(Alias = "encryptionKey")]
  public string? EncryptionKey { get; set; }

  [JsonPropertyName("includeClaimsInIdToken")]
  [YamlMember(Alias = "includeClaimsInIdToken")]
  public string? IncludeClaimsInIdToken { get; set; }

  [JsonPropertyName("issuerMode")]
  [YamlMember(Alias = "issuerMode")]
  public string? IssuerMode { get; set; }
  [JsonPropertyName("providerOauth2Id")]
  [YamlMember(Alias = "providerOauth2Id")]
  public string? ProviderOauth2Id { get; set; }
  [JsonPropertyName("subMode")]
  [YamlMember(Alias = "subMode")]
  public string? SubMode { get; set; }

  [JsonPropertyName("groupDeleteAction")]
  [YamlMember(Alias = "groupDeleteAction")]
  public string? GroupDeleteAction { get; set; }

  [JsonPropertyName("providerMicrosoftEntraId")]
  [YamlMember(Alias = "providerMicrosoftEntraId")]
  public string? ProviderMicrosoftEntraId { get; set; }

  [JsonPropertyName("tenantId")]
  [YamlMember(Alias = "tenantId")]
  public string? TenantId { get; set; }
  [JsonPropertyName("userDeleteAction")]
  [YamlMember(Alias = "userDeleteAction")]
  public string? UserDeleteAction { get; set; }
  [JsonPropertyName("baseDn")]
  [YamlMember(Alias = "baseDn")]
  public string? BaseDn { get; set; }
  [JsonPropertyName("bindFlow")]
  [YamlMember(Alias = "bindFlow")]
  public string? BindFlow { get; set; }
  [JsonPropertyName("bindMode")]
  [YamlMember(Alias = "bindMode")]
  public string? BindMode { get; set; }
  [JsonPropertyName("certificate")]
  [YamlMember(Alias = "certificate")]
  public string? Certificate { get; set; }
  [JsonPropertyName("gidStartNumber")]
  [YamlMember(Alias = "gidStartNumber")]
  public string? GidStartNumber { get; set; }
  [JsonPropertyName("providerLdapId")]
  [YamlMember(Alias = "providerLdapId")]
  public string? ProviderLdapId { get; set; }
  [JsonPropertyName("searchMode")]
  [YamlMember(Alias = "searchMode")]
  public string? SearchMode { get; set; }
  [JsonPropertyName("tlsServerName")]
  [YamlMember(Alias = "tlsServerName")]
  public string? TlsServerName { get; set; }
  [JsonPropertyName("uidStartNumber")]
  [YamlMember(Alias = "uidStartNumber")]
  public string? UidStartNumber { get; set; }
  [JsonPropertyName("unbindFlow")]
  [YamlMember(Alias = "unbindFlow")]
  public string? UnbindFlow { get; set; }
  [JsonPropertyName("credentials")]
  [YamlMember(Alias = "credentials")]
  public string? Credentials { get; set; }

  [JsonPropertyName("defaultGroupEmailDomain")]
  [YamlMember(Alias = "defaultGroupEmailDomain")]
  public string? DefaultGroupEmailDomain { get; set; }

  [JsonPropertyName("delegatedSubject")]
  [YamlMember(Alias = "delegatedSubject")]
  public string? DelegatedSubject { get; set; }

  [JsonPropertyName("providerGoogleWorkspaceId")]
  [YamlMember(Alias = "providerGoogleWorkspaceId")]
  public string? ProviderGoogleWorkspaceId { get; set; }
}
