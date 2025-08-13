using System.Collections.Immutable;

namespace applications.Models.Authentik;

public sealed class AuthentikProviderMicrosoftEntra
{
  public string ClientId { get; set; } = null!;
  public string ClientSecret { get; set; } = null!;
  public bool? DryRun { get; set; }
  public bool? ExcludeUsersServiceAccount { get; set; }
  public string? FilterGroup { get; set; }
  public string? GroupDeleteAction { get; set; }
  public ImmutableList<string>? PropertyMappings { get; set; }
  public ImmutableList<string>? PropertyMappingsGroups { get; set; }
  public string? ProviderMicrosoftEntraId { get; set; }
  public string TenantId { get; set; } = null!;
  public string? UserDeleteAction { get; set; }
}
