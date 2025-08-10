using System.Collections.Immutable;

namespace Models.Authentik;

public sealed class AuthentikProviderProxy
{
  public string? AccessTokenValidity { get; set; }
  public string? AuthenticationFlow { get; set; }
  public string? AuthorizationFlow { get; set; }
  public bool? BasicAuthEnabled { get; set; }
  public string? BasicAuthPasswordAttribute { get; set; }
  public string? BasicAuthUsernameAttribute { get; set; }
  public string? CookieDomain { get; set; }
  public string? ExternalHost { get; set; }
  public bool? InterceptHeaderAuth { get; set; }
  public string? InternalHost { get; set; }
  public bool? InternalHostSslValidation { get; set; }
  public string? InvalidationFlow { get; set; }
  public ImmutableList<string>? JwksSources { get; set; }
  public ImmutableList<double>? JwtFederationProviders { get; set; }
  public ImmutableList<string>? JwtFederationSources { get; set; }
  public string? Mode { get; set; }
  public string? Name { get; set; }
  public ImmutableList<string>? PropertyMappings { get; set; }
  public string? ProviderProxyId { get; set; }
  public string? RefreshTokenValidity { get; set; }
  public string? SkipPathRegex { get; set; }
}
