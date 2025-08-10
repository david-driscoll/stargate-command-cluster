using System.Collections.Generic;
using Models;
using Models.UptimeKuma.Resources;
using Pulumi;
using Pulumi.Authentik;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using static applications.Mappings;

namespace applications;

public class KumaGroups : Pulumi.ComponentResource
{
  private Dictionary<string, CustomResource> groups = new();
  public IReadOnlyDictionary<string, CustomResource> Groups => groups;
  private readonly IReadOnlyCollection<(string GroupName, string? ParentName)> initialGroups = [
    (Constants.Groups.System, null),
    (Constants.Groups.Applications, null),
  ];
  public KumaGroups(ComponentResourceOptions? options = null) : base("custom:resource:KumaGrouops", "kuma-groups", options)
  {
    foreach (var group in initialGroups)
    {
      AddGroup(group.GroupName, group.ParentName);
    }
  }

  public Output<string> AddGroup(string groupName, string? parentName = null)
  {
    if (groups.TryGetValue(groupName, out var group)) return group.Id;
    var groupResource = new KumaUptimeResourceArgs()
    {
      Metadata = new ObjectMetaArgs()
      {
        Name = groupName,
      },
      Spec = new KumaUptimeResourceSpecArgs()
      {
        Config = parentName is {} ? new KumaUptimeResourceConfigArgs()
        {
          Name = groupName,
          ParentName = parentName
        } : new KumaUptimeResourceConfigArgs()
        {
          Name = groupName
        }
      },
    };
    var customResource = new Pulumi.Kubernetes.ApiExtensions.CustomResource(PostfixName(groupName), groupResource, new CustomResourceOptions()
    {
Parent = this,
    });
    groups[PostfixTitle(groupName)] = customResource;
    return customResource.Id;
  }
}

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
