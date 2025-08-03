using System.Collections.Generic;
using k8s;
using k8s.Models;

public class KumaResourceList : KubernetesObject
{
  public V1ListMeta Metadata { get; set; }
  public List<KumaResource> Items { get; set; }
}