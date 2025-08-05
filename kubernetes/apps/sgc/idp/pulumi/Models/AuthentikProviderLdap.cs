namespace authentik.Models;

public sealed class AuthentikProviderLdap
{
  public string BaseDn { get; set; } = null!;
  public string BindFlow { get; set; } = null!;
  public string? BindMode { get; set; }
  public string? Certificate { get; set; }
  public double? GidStartNumber { get; set; }
  public bool? MfaSupport { get; set; }
  public string? ProviderLdapId { get; set; }
  public string? SearchMode { get; set; }
  public string? TlsServerName { get; set; }
  public double? UidStartNumber { get; set; }
  public string UnbindFlow { get; set; } = null!;
}