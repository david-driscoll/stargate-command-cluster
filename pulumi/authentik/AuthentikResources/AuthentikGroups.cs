using System;
using System.Collections.Generic;
using models;
using Pulumi;
using Pulumi.Authentik;

namespace authentik.AuthentikResources;

public class AuthentikGroups : Pulumi.ComponentResource
{
  private readonly IReadOnlyCollection<(string GroupName, string? ParentName)> _initialGroups =
  [
    (Constants.Roles.User, null),
    (Constants.Roles.Admin, Constants.Roles.User),
    (Constants.Roles.Family, Constants.Roles.User),
    (Constants.Roles.Friend, Constants.Roles.User),
    (Constants.Roles.MediaManager, Constants.Roles.User)
  ];

  public AuthentikGroups(ComponentResourceOptions? options = null) : base("custom:resource:AuthentikGroups",
    "authentik-groups", options)
  {
    foreach (var group in _initialGroups)
    {
      var roleResource = new RbacRole(group.GroupName, new()
      {
        Name = group.GroupName,
      }, new CustomResourceOptions() { Parent = this });
      _roles[group.GroupName] = roleResource;
      var groupResource = new Group(group.GroupName, _groups.TryGetValue(group.ParentName ?? "", out var parentGroup) ? new()
      {
        Name = group.GroupName,
        Roles = [roleResource.RbacRoleId],
        IsSuperuser = group.GroupName == Constants.Roles.Admin,
        Parent = parentGroup.GroupId,
      } : new()
      {
        Name = group.GroupName,
        Roles = [roleResource.RbacRoleId],
        IsSuperuser = group.GroupName == Constants.Roles.Admin,
      }, new CustomResourceOptions() { Parent = this });
      _groups[group.GroupName] = groupResource;
    }
  }

  private readonly Dictionary<string, Group> _groups = new(StringComparer.OrdinalIgnoreCase);
  public Group GetGroup(string groupName) => _groups.TryGetValue(groupName, out var group) ? group : throw new KeyNotFoundException($"Group '{groupName}' not found.");
  private readonly Dictionary<string, RbacRole> _roles = new(StringComparer.OrdinalIgnoreCase);
  public RbacRole GetRole(string roleName) => _roles.TryGetValue(roleName, out var role) ? role : throw new KeyNotFoundException($"Role '{roleName}' not found.");
}
