using System.Collections.Immutable;

namespace applications.Models.Authentik;

public sealed class AuthentikProviderOauth2
{
  public string? AccessCodeValidity { get; set; }
  public string? AccessTokenValidity { get; set; }
  public ImmutableList<string>? AllowedRedirectUris { get; set; }
  public string? AuthenticationFlow { get; set; }
  public string AuthorizationFlow { get; set; } = null!;
  public string ClientId { get; set; } = null!;
  public string? ClientSecret { get; set; }
  public string? ClientType { get; set; }
  public string? EncryptionKey { get; set; }
  public bool? IncludeClaimsInIdToken { get; set; }
  public string InvalidationFlow { get; set; } = null!;
  public string? IssuerMode { get; set; }
  public ImmutableList<string>? JwksSources { get; set; }
  public ImmutableList<double>? JwtFederationProviders { get; set; }
  public ImmutableList<string>? JwtFederationSources { get; set; }
  public ImmutableList<string>? PropertyMappings { get; set; }
  public string? ProviderOauth2Id { get; set; }
  public string? RefreshTokenValidity { get; set; }
  public string? SigningKey { get; set; }
  public string? SubMode { get; set; }
}
