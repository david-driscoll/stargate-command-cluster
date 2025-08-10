using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Humanizer;
using k8s;
using Models;
using Models.ApplicationDefinition;
using Pulumi;
using Pulumi.Authentik;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using CustomResource = Pulumi.Kubernetes.ApiExtensions.CustomResource;

namespace applications;

public class KumaUptimeResources : ComponentResource
{
  public class Args : ResourceArgs
  {
    public required KumaGroups Groups { get; init; }
    public required Kubernetes Cluster { get; init; }
  }

  public KumaUptimeResources(string name, Args args,
    ComponentResourceOptions? options = null) : base("custom:resource:ClusterApplicationResources",
    name, args, options)
  {
    var applications = Output.Create(PopulateCluster.GetApplications(args.Cluster))
      .Apply(applications =>
      {
        foreach (var application in applications
                   .Where(z => z.Spec.Uptime is { })
                   .OrderBy(z => KumaUptimeModelMapper.GetUptime(z.Spec.Uptime).ParentName is null)
                )
        {
          CreateResource(args, application);
        }

        return applications;
      });
  }

  private CustomResource CreateResource(Args args, ApplicationDefinition application)
  {
    Debug.Assert(application.Spec.Uptime != null);
    var (clusterName, clusterTitle) = application.GetClusterNameAndTitle();
    var uptime = KumaUptimeModelMapper.GetUptime(application.Spec.Uptime);
    var config = new KumaUptimeResourceConfigArgs()
    {
      Name = application.Spec.Name,
      Description = application.Spec.Description ?? "",
      Active = true,
    };
    KumaUptimeModelMapper.MapUptime(config, application.Spec.Uptime);
    if (uptime.ParentName is "cluster")
    {
      config.ParentName = args.Groups.AddGroup(clusterName, clusterTitle);
    } else if (args.Groups.Groups.TryGetValue(uptime.ParentName ?? "", out var g))
    {
      config.ParentName = g.Id;
    }
    else
    {
      throw new KeyNotFoundException(
        $"Parent group '{uptime.ParentName}' not found for uptime resource '{application.Spec.Name}'");
    }

    return new CustomResource(application.Metadata.Name, new KumaUptimeResourceArgs()
    {
      Metadata = new ObjectMetaArgs()
      {
        Name = application.Metadata.Name,
        Namespace = "observability",
        Labels = new Dictionary<string, string>
        {
          ["driscoll.dev/cluster"] = clusterName,
          ["driscoll.dev/clusterTitle"] = clusterTitle
        }
      },
      Spec = new KumaUptimeResourceSpecArgs { Config = config }
    }, new CustomResourceOptions() { Parent = this });
  }
}
