using System.Collections.Generic;
using Pulumi;
using Pulumi.Authentik;
using static Mappings;

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
      var roleResource = new RbacRole(PostfixName(group.GroupName), new()
      {
        Name = PostfixTitle(group.GroupName),
      });
      roles[PostfixTitle(group.GroupName)] = roleResource;
      var groupResource = new Group(PostfixName(group.GroupName), new()
      {
        Name = PostfixTitle(group.GroupName),
        Roles = [roleResource.Id],
        IsSuperuser = group.GroupName == "Admin",
        Parent = groups.TryGetValue(PostfixName(group.ParentName ?? ""), out var parentGroup) ? parentGroup.Id : Output.Create((string)null),
      });
      groups[PostfixTitle(group.GroupName)] = groupResource;
    }
  }

  private Dictionary<string, Group> groups = new();
  public IReadOnlyDictionary<string, Group> Groups => groups;
  private Dictionary<string, RbacRole> roles = new();
  public IReadOnlyDictionary<string, RbacRole> Roles => roles;
}
