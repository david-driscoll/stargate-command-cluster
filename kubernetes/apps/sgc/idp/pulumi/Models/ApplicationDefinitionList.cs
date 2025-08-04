using System.Collections.Generic;
using k8s;
using k8s.Models;

namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public class ApplicationDefinitionList : KubernetesObject, IMetadata<V1ListMeta>
{
  public V1ListMeta Metadata { get; set; }
  public List<ApplicationDefinition> Items { get; set; }
}