using System.Collections.Immutable;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public sealed class AuthentikProviderSaml
{
  public string AcsUrl { get; set; } = null!;
  public string? AssertionValidNotBefore { get; set; }
  public string? AssertionValidNotOnOrAfter { get; set; }
  public string? Audience { get; set; }
  public string? AuthenticationFlow { get; set; }
  public string? AuthnContextClassRefMapping { get; set; }
  public string AuthorizationFlow { get; set; } = null!;
  public string? DefaultRelayState { get; set; }
  public string? DigestAlgorithm { get; set; }
  public string? EncryptionKp { get; set; }
  public string InvalidationFlow { get; set; } = null!;
  public string? Issuer { get; set; }
  public string? NameIdMapping { get; set; }
  public ImmutableList<string>? PropertyMappings { get; set; }
  public string? ProviderSamlId { get; set; }
  public string? SessionValidNotOnOrAfter { get; set; }
  public bool? SignAssertion { get; set; }
  public bool? SignResponse { get; set; }
  public string? SignatureAlgorithm { get; set; }
  public string? SigningKp { get; set; }
  public string? SpBinding { get; set; }
  public string? UrlSloPost { get; set; }
  public string? UrlSloRedirect { get; set; }
  public string? UrlSsoInit { get; set; }
  public string? UrlSsoPost { get; set; }
  public string? UrlSsoRedirect { get; set; }
  public string? VerificationKp { get; set; }
}