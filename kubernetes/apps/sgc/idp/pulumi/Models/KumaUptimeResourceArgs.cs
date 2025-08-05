using System;
using Pulumi;
using Pulumi.Kubernetes.ApiExtensions;

namespace authentik.Models;

class KumaUptimeResourceArgs() : CustomResourceArgs("autokuma.bigboot.dev/v1", "KumaEntity")
{
  [Input("spec")] public KumaUptimeResourceSpecArgs Spec { get; set; } = new();

  protected void ValidateMember(Type memberType, string fullName) { }
}
