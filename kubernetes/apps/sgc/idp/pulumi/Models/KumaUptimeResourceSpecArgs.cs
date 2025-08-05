using System;
using Pulumi;

namespace authentik.Models;

class KumaUptimeResourceSpecArgs : ResourceArgs
{
  [Input("config")]
  public KumaUptimeResourceConfigArgs Config { get; set; } = new();
}
