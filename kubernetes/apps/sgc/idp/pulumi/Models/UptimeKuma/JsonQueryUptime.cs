using System.Collections.Immutable;

namespace Models.UptimeKuma;

public class JsonQueryUptime : UptimeBase
{
  public override string Type { get; } = "json-query";
  [YamlDotNet.Serialization.YamlMember(Alias = "url")]
  [System.Text.Json.Serialization.JsonPropertyName("url")]
  public string Url { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "json_path")]
  [System.Text.Json.Serialization.JsonPropertyName("json_path")]
  public string JsonPath { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "expected_value")]
  [System.Text.Json.Serialization.JsonPropertyName("expected_value")]
  public string ExpectedValue { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "method")]
  [System.Text.Json.Serialization.JsonPropertyName("method")]
  public string Method { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "accepted_statuscodes")]
  [System.Text.Json.Serialization.JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "auth_domain")]
  [System.Text.Json.Serialization.JsonPropertyName("auth_domain")]
  public string? AuthDomain { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "auth_method")]
  [System.Text.Json.Serialization.JsonPropertyName("auth_method")]
  public string? AuthMethod { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "auth_workstation")]
  [System.Text.Json.Serialization.JsonPropertyName("auth_workstation")]
  public string? AuthWorkstation { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "basic_auth_user")]
  [System.Text.Json.Serialization.JsonPropertyName("basic_auth_user")]
  public string? BasicAuthUser { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "basic_auth_pass")]
  [System.Text.Json.Serialization.JsonPropertyName("basic_auth_pass")]
  public string? BasicAuthPass { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "body")]
  [System.Text.Json.Serialization.JsonPropertyName("body")]
  public string? Body { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "expiry_notification")]
  [System.Text.Json.Serialization.JsonPropertyName("expiry_notification")]
  public bool? ExpiryNotification { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "http_body_encoding")]
  [System.Text.Json.Serialization.JsonPropertyName("http_body_encoding")]
  public string? HttpBodyEncoding { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "ignore_tls")]
  [System.Text.Json.Serialization.JsonPropertyName("ignore_tls")]
  public bool? IgnoreTls { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "max_redirects")]
  [System.Text.Json.Serialization.JsonPropertyName("max_redirects")]
  public int? MaxRedirects { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "oauth_auth_method")]
  [System.Text.Json.Serialization.JsonPropertyName("oauth_auth_method")]
  public string? OauthAuthMethod { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "oauth_client_id")]
  [System.Text.Json.Serialization.JsonPropertyName("oauth_client_id")]
  public string? OauthClientId { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "oauth_client_secret")]
  [System.Text.Json.Serialization.JsonPropertyName("oauth_client_secret")]
  public string? OauthClientSecret { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "oauth_scopes")]
  [System.Text.Json.Serialization.JsonPropertyName("oauth_scopes")]
  public string? OauthScopes { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "oauth_token_url")]
  [System.Text.Json.Serialization.JsonPropertyName("oauth_token_url")]
  public string? OauthTokenUrl { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "proxy_id")]
  [System.Text.Json.Serialization.JsonPropertyName("proxy_id")]
  public string? ProxyId { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "resend_interval")]
  [System.Text.Json.Serialization.JsonPropertyName("resend_interval")]
  public int? ResendInterval { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "timeout")]
  [System.Text.Json.Serialization.JsonPropertyName("timeout")]
  public int? Timeout { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "tls_ca")]
  [System.Text.Json.Serialization.JsonPropertyName("tls_ca")]
  public string? TlsCa { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "tls_cert")]
  [System.Text.Json.Serialization.JsonPropertyName("tls_cert")]
  public string? TlsCert { get; init; }
  [YamlDotNet.Serialization.YamlMember(Alias = "tls_key")]
  [System.Text.Json.Serialization.JsonPropertyName("tls_key")]
  public string? TlsKey { get; init; }
}
