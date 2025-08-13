using Pulumi;
using Pulumi.Kubernetes.ApiExtensions;

namespace applications.PulumiModels;

class KumaUptimeResourceArgs() : CustomResourceArgs("autokuma.bigboot.dev/v1", "KumaEntity")
{
  [Input("spec")] public KumaUptimeResourceSpecArgs Spec { get; set; } = new();
}
