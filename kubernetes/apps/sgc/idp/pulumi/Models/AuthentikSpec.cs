using System.Text.Json.Serialization;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public record AuthentikSpec
{
  [JsonPropertyName("type")] public string? Type { get; set; }
  [JsonPropertyName("eventRetention")] public string? EventRetention { get; set; }

  [JsonPropertyName("jwtFederationProviders")]
  public string? JwtFederationProviders { get; set; }

  [JsonPropertyName("providerSsfId")] public string? ProviderSsfId { get; set; }
  [JsonPropertyName("signingKey")] public string? SigningKey { get; set; }

  [JsonPropertyName("compatibilityMode")]
  public string? CompatibilityMode { get; set; }

  [JsonPropertyName("dryRun")] public string? DryRun { get; set; }

  [JsonPropertyName("excludeUsersServiceAccount")]
  public string? ExcludeUsersServiceAccount { get; set; }

  [JsonPropertyName("filterGroup")] public string? FilterGroup { get; set; }
  [JsonPropertyName("propertyMappings")] public string? PropertyMappings { get; set; }

  [JsonPropertyName("propertyMappingsGroups")]
  public string? PropertyMappingsGroups { get; set; }

  [JsonPropertyName("providerScimId")] public string? ProviderScimId { get; set; }
  [JsonPropertyName("token")] public string? Token { get; set; }
  [JsonPropertyName("url")] public string? Url { get; set; }
  [JsonPropertyName("acsUrl")] public string? AcsUrl { get; set; }

  [JsonPropertyName("assertionValidNotBefore")]
  public string? AssertionValidNotBefore { get; set; }

  [JsonPropertyName("assertionValidNotOnOrAfter")]
  public string? AssertionValidNotOnOrAfter { get; set; }

  [JsonPropertyName("audience")] public string? Audience { get; set; }

  [JsonPropertyName("authenticationFlow")]
  public string? AuthenticationFlow { get; set; }

  [JsonPropertyName("authnContextClassRefMapping")]
  public string? AuthnContextClassRefMapping { get; set; }

  [JsonPropertyName("authorizationFlow")]
  public string? AuthorizationFlow { get; set; }

  [JsonPropertyName("defaultRelayState")]
  public string? DefaultRelayState { get; set; }

  [JsonPropertyName("digestAlgorithm")] public string? DigestAlgorithm { get; set; }
  [JsonPropertyName("encryptionKp")] public string? EncryptionKp { get; set; }
  [JsonPropertyName("invalidationFlow")] public string? InvalidationFlow { get; set; }
  [JsonPropertyName("issuer")] public string? Issuer { get; set; }
  [JsonPropertyName("nameIdMapping")] public string? NameIdMapping { get; set; }
  [JsonPropertyName("providerSamlId")] public string? ProviderSamlId { get; set; }

  [JsonPropertyName("sessionValidNotOnOrAfter")]
  public string? SessionValidNotOnOrAfter { get; set; }

  [JsonPropertyName("signAssertion")] public string? SignAssertion { get; set; }
  [JsonPropertyName("signResponse")] public string? SignResponse { get; set; }

  [JsonPropertyName("signatureAlgorithm")]
  public string? SignatureAlgorithm { get; set; }

  [JsonPropertyName("signingKp")] public string? SigningKp { get; set; }
  [JsonPropertyName("spBinding")] public string? SpBinding { get; set; }
  [JsonPropertyName("urlSloPost")] public string? UrlSloPost { get; set; }
  [JsonPropertyName("urlSloRedirect")] public string? UrlSloRedirect { get; set; }
  [JsonPropertyName("urlSsoInit")] public string? UrlSsoInit { get; set; }
  [JsonPropertyName("urlSsoPost")] public string? UrlSsoPost { get; set; }
  [JsonPropertyName("urlSsoRedirect")] public string? UrlSsoRedirect { get; set; }
  [JsonPropertyName("verificationKp")] public string? VerificationKp { get; set; }

  [JsonPropertyName("accessTokenValidity")]
  public string? AccessTokenValidity { get; set; }

  [JsonPropertyName("basicAuthEnabled")] public string? BasicAuthEnabled { get; set; }

  [JsonPropertyName("basicAuthPasswordAttribute")]
  public string? BasicAuthPasswordAttribute { get; set; }

  [JsonPropertyName("basicAuthUsernameAttribute")]
  public string? BasicAuthUsernameAttribute { get; set; }

  [JsonPropertyName("cookieDomain")] public string? CookieDomain { get; set; }
  [JsonPropertyName("externalHost")] public string? ExternalHost { get; set; }

  [JsonPropertyName("interceptHeaderAuth")]
  public string? InterceptHeaderAuth { get; set; }

  [JsonPropertyName("internalHost")] public string? InternalHost { get; set; }

  [JsonPropertyName("internalHostSslValidation")]
  public string? InternalHostSslValidation { get; set; }

  [JsonPropertyName("jwksSources")] public string? JwksSources { get; set; }

  [JsonPropertyName("jwtFederationSources")]
  public string? JwtFederationSources { get; set; }

  [JsonPropertyName("mode")] public string? Mode { get; set; }
  [JsonPropertyName("providerProxyId")] public string? ProviderProxyId { get; set; }

  [JsonPropertyName("refreshTokenValidity")]
  public string? RefreshTokenValidity { get; set; }

  [JsonPropertyName("skipPathRegex")] public string? SkipPathRegex { get; set; }
  [JsonPropertyName("clientNetworks")] public string? ClientNetworks { get; set; }
  [JsonPropertyName("mfaSupport")] public string? MfaSupport { get; set; }
  [JsonPropertyName("providerRadiusId")] public string? ProviderRadiusId { get; set; }
  [JsonPropertyName("sharedSecret")] public string? SharedSecret { get; set; }
  [JsonPropertyName("connectionExpiry")] public string? ConnectionExpiry { get; set; }
  [JsonPropertyName("providerRacId")] public string? ProviderRacId { get; set; }
  [JsonPropertyName("settings")] public string? Settings { get; set; }

  [JsonPropertyName("accessCodeValidity")]
  public string? AccessCodeValidity { get; set; }

  [JsonPropertyName("allowedRedirectUris")]
  public string? AllowedRedirectUris { get; set; }

  [JsonPropertyName("clientId")] public string? ClientId { get; set; }
  [JsonPropertyName("clientSecret")] public string? ClientSecret { get; set; }
  [JsonPropertyName("clientType")] public string? ClientType { get; set; }
  [JsonPropertyName("encryptionKey")] public string? EncryptionKey { get; set; }

  [JsonPropertyName("includeClaimsInIdToken")]
  public string? IncludeClaimsInIdToken { get; set; }

  [JsonPropertyName("issuerMode")] public string? IssuerMode { get; set; }
  [JsonPropertyName("providerOauth2Id")] public string? ProviderOauth2Id { get; set; }
  [JsonPropertyName("subMode")] public string? SubMode { get; set; }

  [JsonPropertyName("groupDeleteAction")]
  public string? GroupDeleteAction { get; set; }

  [JsonPropertyName("providerMicrosoftEntraId")]
  public string? ProviderMicrosoftEntraId { get; set; }

  [JsonPropertyName("tenantId")] public string? TenantId { get; set; }
  [JsonPropertyName("userDeleteAction")] public string? UserDeleteAction { get; set; }
  [JsonPropertyName("baseDn")] public string? BaseDn { get; set; }
  [JsonPropertyName("bindFlow")] public string? BindFlow { get; set; }
  [JsonPropertyName("bindMode")] public string? BindMode { get; set; }
  [JsonPropertyName("certificate")] public string? Certificate { get; set; }
  [JsonPropertyName("gidStartNumber")] public string? GidStartNumber { get; set; }
  [JsonPropertyName("providerLdapId")] public string? ProviderLdapId { get; set; }
  [JsonPropertyName("searchMode")] public string? SearchMode { get; set; }
  [JsonPropertyName("tlsServerName")] public string? TlsServerName { get; set; }
  [JsonPropertyName("uidStartNumber")] public string? UidStartNumber { get; set; }
  [JsonPropertyName("unbindFlow")] public string? UnbindFlow { get; set; }
  [JsonPropertyName("credentials")] public string? Credentials { get; set; }

  [JsonPropertyName("defaultGroupEmailDomain")]
  public string? DefaultGroupEmailDomain { get; set; }

  [JsonPropertyName("delegatedSubject")] public string? DelegatedSubject { get; set; }

  [JsonPropertyName("providerGoogleWorkspaceId")]
  public string? ProviderGoogleWorkspaceId { get; set; }
}
