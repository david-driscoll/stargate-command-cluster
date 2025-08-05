using System.Collections.Immutable;

namespace authentik.Models;

public record RealBrowserUptime : UptimeBase
{
  public override string Type { get; } = "real-browser";
  public string RemoteBrowser { get; init; }
  public bool? RemoteBrowsersToggle { get; init; }
  public string Url { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;
}