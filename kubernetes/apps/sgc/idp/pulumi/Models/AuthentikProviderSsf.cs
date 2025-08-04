using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public sealed class AuthentikProviderSsf
{
  public string? EventRetention { get; set; }
  public ImmutableList<double>? JwtFederationProviders { get; set; }
  public string? ProviderSsfId { get; set; }
  public string? SigningKey { get; set; }
}