using Pulumi;

namespace applications.PulumiModels;

class KumaUptimeResourceSpecArgs : ResourceArgs
{
  [Input("config")]
  public KumaUptimeResourceConfigArgs Config { get; set; }
}
