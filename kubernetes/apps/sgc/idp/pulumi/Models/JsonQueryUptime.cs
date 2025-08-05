using System.Collections.Immutable;

namespace authentik.Models;

public record JsonQueryUptime : UptimeBase
{
  public override string Type { get; } = "json-query";
  public string Url { get; init; }
  public string JsonPath { get; init; }
  public string ExpectedValue { get; init; }
  public string Method { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
  public string? AuthDomain { get; init; }
  public string? AuthMethod { get; init; }
  public string? AuthWorkstation { get; init; }
  public string? BasicAuthUser { get; init; }
  public string? BasicAuthPass { get; init; }
  public string? Body { get; init; }

  public bool? ExpiryNotification { get; init; }
  public string? HttpBodyEncoding { get; init; }
  public bool? IgnoreTls { get; init; }
  public int? MaxRedirects { get; init; }
  public string? OauthAuthMethod { get; init; }
  public string? OauthClientId { get; init; }
  public string? OauthClientSecret { get; init; }
  public string? OauthScopes { get; init; }
  public string? OauthTokenUrl { get; init; }
  public string? ProxyId { get; init; }
  public int? ResendInterval { get; init; }
  public int? Timeout { get; init; }
  public string? TlsCa { get; init; }
  public string? TlsCert { get; init; }
  public string? TlsKey { get; init; }
}