using Pulumi;

namespace Models;

class KumaUptimeResourceSpecArgs : ResourceArgs
{
  [Input("config")]
  public KumaUptimeResourceConfigArgs Config { get; set; }
}
