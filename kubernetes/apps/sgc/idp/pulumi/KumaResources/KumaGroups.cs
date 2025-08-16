using System.Collections.Generic;
using applications.PulumiModels;
using Pulumi;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;

namespace applications.KumaResources;

public class KumaGroups : Pulumi.ComponentResource
{
  private readonly Dictionary<string, CustomResource> _groups = new();

  private readonly IReadOnlyCollection<(string GroupName, string GroupTitle, string? ParentName)> _initialGroups =
  [
    ("system", Constants.Groups.System, null),
    ("apps", Constants.Groups.Applications, null),
  ];

  public KumaGroups(ComponentResourceOptions? options = null) : base("custom:resource:KumaGrouops", "kuma-groups",
    options)
  {
    foreach (var group in _initialGroups)
    {
      AddGroup(group.GroupName, group.GroupTitle, group.ParentName);
    }
  }

  public CustomResource GetGroup(string? groupName) => _groups.TryGetValue(groupName, out var group)
    ? group
    : throw new KeyNotFoundException($"Group '{groupName}' not found.");

  public CustomResource AddGroup(string groupName, string groupTitle, string? parentName = null)
  {
    if (_groups.TryGetValue(groupName, out var group)) return group;
    var groupResource = new KumaUptimeResourceArgs()
    {
      Metadata = new ObjectMetaArgs()
      {
        Name = groupName,
      },
      Spec = new KumaUptimeResourceSpecArgs()
      {
        Config = parentName is { }
          ? new KumaUptimeResourceConfigArgs()
          {
            Name = groupTitle,
            Type = "group",
            Interval = 30,
            ParentName = parentName
          }
          : new KumaUptimeResourceConfigArgs()
          {
            Name = groupTitle,
            Type = "group",
            Interval = 30,
          }
      },
    };
    var customResource = new Pulumi.Kubernetes.ApiExtensions.CustomResource(groupName, groupResource,
      new CustomResourceOptions()
      {
        Parent = this,
      });
    _groups[groupName] = customResource;
    return customResource;
  }
}
