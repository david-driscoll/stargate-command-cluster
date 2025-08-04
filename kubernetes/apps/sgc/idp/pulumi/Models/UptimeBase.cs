namespace StargateCommandCluster.Kubernetes.Apps.Sgc.Idp.Pulumi;

public abstract record UptimeBase
{
  public abstract string Type { get; }
  public bool Active { get; init; } = true;
  public int? Interval { get; init; } = 5 * 60;
  public int? MaxRetries { get; init; } = 3;
  public string? ParentName { get; init; }
  public int? RetryInterval { get; init; } = 60;
  public bool UpsideDown { get; init; }
}