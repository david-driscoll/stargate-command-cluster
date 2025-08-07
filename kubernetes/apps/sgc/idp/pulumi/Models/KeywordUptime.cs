using System.Collections.Immutable;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace authentik.Models;

public class KeywordUptime : UptimeBase
{
  public override string Type { get; } = "keyword";
  [YamlMember(Alias = "url")]
  [JsonPropertyName("url")]
  public string Url { get; set; }
  [YamlMember(Alias = "keyword")]
  [JsonPropertyName("keyword")]
  public string Keyword { get; set; }
  [YamlMember(Alias = "invert_keyword")]
  [JsonPropertyName("invert_keyword")]
  public bool? InvertKeyword { get; set; }
  [YamlMember(Alias = "method")]
  [JsonPropertyName("method")]
  public string Method { get; init; }
  [YamlMember(Alias = "accepted_statuscodes")]
  [JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
  [YamlMember(Alias = "auth_domain")]
  [JsonPropertyName("auth_domain")]
  public string? AuthDomain { get; init; }
  [YamlMember(Alias = "auth_method")]
  [JsonPropertyName("auth_method")]
  public string? AuthMethod { get; init; }
  [YamlMember(Alias = "auth_workstation")]
  [JsonPropertyName("auth_workstation")]
  public string? AuthWorkstation { get; init; }
  [YamlMember(Alias = "basic_auth_user")]
  [JsonPropertyName("basic_auth_user")]
  public string? BasicAuthUser { get; init; }
  [YamlMember(Alias = "basic_auth_pass")]
  [JsonPropertyName("basic_auth_pass")]
  public string? BasicAuthPass { get; init; }
  [YamlMember(Alias = "body")]
  [JsonPropertyName("body")]
  public string? Body { get; init; }
  [YamlMember(Alias = "expiry_notification")]
  [JsonPropertyName("expiry_notification")]
  public bool? ExpiryNotification { get; init; }
  [YamlMember(Alias = "http_body_encoding")]
  [JsonPropertyName("http_body_encoding")]
  public string? HttpBodyEncoding { get; init; }
  [YamlMember(Alias = "ignore_tls")]
  [JsonPropertyName("ignore_tls")]
  public bool? IgnoreTls { get; init; }
  [YamlMember(Alias = "max_redirects")]
  [JsonPropertyName("max_redirects")]
  public int? MaxRedirects { get; init; }
  [YamlMember(Alias = "oauth_auth_method")]
  [JsonPropertyName("oauth_auth_method")]
  public string? OauthAuthMethod { get; init; }
  [YamlMember(Alias = "oauth_client_id")]
  [JsonPropertyName("oauth_client_id")]
  public string? OauthClientId { get; init; }
  [YamlMember(Alias = "oauth_client_secret")]
  [JsonPropertyName("oauth_client_secret")]
  public string? OauthClientSecret { get; init; }
  [YamlMember(Alias = "oauth_scopes")]
  [JsonPropertyName("oauth_scopes")]
  public string? OauthScopes { get; init; }
  [YamlMember(Alias = "oauth_token_url")]
  [JsonPropertyName("oauth_token_url")]
  public string? OauthTokenUrl { get; init; }
  [YamlMember(Alias = "proxy_id")]
  [JsonPropertyName("proxy_id")]
  public string? ProxyId { get; init; }
  [YamlMember(Alias = "resend_interval")]
  [JsonPropertyName("resend_interval")]
  public int? ResendInterval { get; init; }
  [YamlMember(Alias = "timeout")]
  [JsonPropertyName("timeout")]
  public int? Timeout { get; init; }
  [YamlMember(Alias = "tls_ca")]
  [JsonPropertyName("tls_ca")]
  public string? TlsCa { get; init; }
  [YamlMember(Alias = "tls_cert")]
  [JsonPropertyName("tls_cert")]
  public string? TlsCert { get; init; }
  [YamlMember(Alias = "tls_key")]
  [JsonPropertyName("tls_key")]
  public string? TlsKey { get; init; }
}
