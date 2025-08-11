using System.Collections.Generic;
using Models;
using Pulumi;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;

namespace applications;

public class KumaGroups : Pulumi.ComponentResource
{
  private readonly Dictionary<string, CustomResource> _groups = new();
  private readonly IReadOnlyCollection<(string GroupName, string? ParentName)> _initialGroups = [
    (Constants.Groups.System, null),
    (Constants.Groups.Applications, null),
  ];
  public KumaGroups(ComponentResourceOptions? options = null) : base("custom:resource:KumaGrouops", "kuma-groups", options)
  {
    foreach (var group in _initialGroups)
    {
      AddGroup(group.GroupName, group.ParentName);
    }
  }

  public CustomResource GetGroup(string? groupName) => _groups.TryGetValue(groupName, out var group) ? group : throw new KeyNotFoundException($"Group '{groupName}' not found.");

  public Output<string> AddGroup(string groupName, string? parentName = null)
  {
    if (_groups.TryGetValue(groupName, out var group)) return group.Id;
    var groupResource = new KumaUptimeResourceArgs()
    {
      Metadata = new ObjectMetaArgs()
      {
        Name = groupName.ToLowerInvariant(),
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
    var customResource = new Pulumi.Kubernetes.ApiExtensions.CustomResource(Mappings.PostfixName(groupName), groupResource, new CustomResourceOptions()
    {
      Parent = this,
    });
    _groups[Mappings.PostfixName(groupName)] = customResource;
    return customResource.Id;
  }
}
