using System.Collections.Immutable;

namespace Models.Authentik;

public sealed class AuthentikProviderSsf
{
  public string? EventRetention { get; set; }
  public ImmutableList<double>? JwtFederationProviders { get; set; }
  public string? ProviderSsfId { get; set; }
  public string? SigningKey { get; set; }
}
