using System.Collections.Immutable;

namespace authentik.Models;

public sealed class AuthentikProviderGoogleWorkspace
{
  public string? Credentials { get; set; }
  public string DefaultGroupEmailDomain { get; set; } = null!;
  public string? DelegatedSubject { get; set; }
  public bool? DryRun { get; set; }
  public bool? ExcludeUsersServiceAccount { get; set; }
  public string? FilterGroup { get; set; }
  public string? GroupDeleteAction { get; set; }
  public ImmutableList<string>? PropertyMappings { get; set; }
  public ImmutableList<string>? PropertyMappingsGroups { get; set; }
  public string? ProviderGoogleWorkspaceId { get; set; }
  public string? UserDeleteAction { get; set; }
}