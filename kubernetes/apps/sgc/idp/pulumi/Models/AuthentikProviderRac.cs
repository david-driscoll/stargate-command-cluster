using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public sealed class AuthentikProviderRac
{
  public string? AuthenticationFlow { get; set; }
  public string AuthorizationFlow { get; set; } = null!;
  public string? ConnectionExpiry { get; set; }
  public ImmutableList<string>? PropertyMappings { get; set; }
  public string? ProviderRacId { get; set; }
  public string? Settings { get; set; }
}