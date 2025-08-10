using System.Collections.Immutable;

namespace Models.Authentik;

public sealed class AuthentikProviderScim
{
  public string? CompatibilityMode { get; set; }
  public bool? DryRun { get; set; }
  public bool? ExcludeUsersServiceAccount { get; set; }
  public string? FilterGroup { get; set; }
  public ImmutableList<string>? PropertyMappings { get; set; }
  public ImmutableList<string>? PropertyMappingsGroups { get; set; }
  public string? ProviderScimId { get; set; }
  public string Token { get; set; } = null!;
  public string Url { get; set; } = null!;
}
