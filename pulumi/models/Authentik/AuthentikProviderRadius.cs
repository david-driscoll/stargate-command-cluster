using System.Collections.Immutable;

namespace applications.Models.Authentik;

public sealed class AuthentikProviderRadius
{
  public string AuthorizationFlow { get; set; } = null!;
  public string? ClientNetworks { get; set; }
  public string InvalidationFlow { get; set; } = null!;
  public bool? MfaSupport { get; set; }
  public ImmutableList<string>? PropertyMappings { get; set; }
  public string? ProviderRadiusId { get; set; }
  public string SharedSecret { get; set; } = null!;
}
