using System.Collections.Generic;
using Humanizer;
using Pulumi;
using Pulumi.Authentik;

class AuthentikGroups : Pulumi.ComponentResource
{
  private readonly IReadOnlyCollection<(string GroupName, string? ParentName)> initialGroups = [
    (Constants.Roles.User, null),
    (Constants.Roles.Admin, Constants.Roles.User),
    (Constants.Roles.Family, Constants.Roles.User),
    (Constants.Roles.Friend, Constants.Roles.User),
    (Constants.Roles.MediaManager, Constants.Roles.User)
  ];
  public AuthentikGroups(ComponentResourceOptions? options = null) : base("custom:resource:AuthentikGroups", "authentik-groups", options)
  {
    foreach (var group in initialGroups)
    {
      var roleResource = new RbacRole(group.GroupName.ToLowerInvariant().Dasherize(), new()
      {
        Name = group.GroupName,
      });
      roles[group.GroupName] = roleResource;
      var groupResource = new Group(group.GroupName.ToLowerInvariant().Dasherize(), new()
      {
        Name = group.GroupName,
        Roles = [roleResource.Id],
        IsSuperuser = group.GroupName == "Admin",
        Parent = groups.TryGetValue(group.ParentName ?? "", out var parentGroup) ? parentGroup.Id : Output.Create((string)null),
      });
      groups[group.GroupName] = groupResource;
    }
  }

  private Dictionary<string, Group> groups = new();
  public IReadOnlyDictionary<string, Group> Groups => groups;
  private Dictionary<string, RbacRole> roles = new();
  public IReadOnlyDictionary<string, RbacRole> Roles => roles;
}
